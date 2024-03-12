using System.Runtime.CompilerServices;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Papst.EventStore.AzureCosmos.Database;
using Papst.EventStore.Documents;

namespace Papst.EventStore.AzureCosmos;

internal sealed class CosmosEventStream : IEventStream
{
  private EventStreamIndexEntity _stream;
  private readonly ILogger<CosmosEventStream> _logger;
  private readonly CosmosDatabaseProvider _dbProvider;
  private readonly IEventTypeProvider _eventTypeProvider;
  private readonly ICosmosIdStrategy _idStrategy;
  private readonly CosmosEventStoreOptions _options;

  public Guid StreamId => _stream.StreamId;
  public ulong Version => _stream.Version;
  public DateTimeOffset Created => _stream.Created;

  public CosmosEventStream(
    ILogger<CosmosEventStream> logger,
    CosmosEventStoreOptions options,
    EventStreamIndexEntity stream,
    CosmosDatabaseProvider dbProvider,
    IEventTypeProvider eventTypeProvider,
    ICosmosIdStrategy idStrategy
  )
  {
    _logger = logger;
    _options = options;
    _stream = stream;
    _dbProvider = dbProvider;
    _eventTypeProvider = eventTypeProvider;
    _idStrategy = idStrategy;
  }

  public async Task<EventStreamDocument?> GetLatestSnapshot(CancellationToken cancellationToken = default)
  {
    if (!_stream.LatestSnapshotVersion.HasValue)
    {
      return null;
    }

    string snapShotId = await _idStrategy.GenerateIdAsync(
      _stream.StreamId,
      _stream.LatestSnapshotVersion.Value,
      EventStreamDocumentType.Snapshot).ConfigureAwait(false);

    ItemResponse<EventStreamDocumentEntity> result = await _dbProvider.Container
      .ReadItemAsync<EventStreamDocumentEntity>(
        snapShotId,
        new(_stream.StreamId.ToString()),
        cancellationToken: cancellationToken).ConfigureAwait(false);

    return Map(result.Resource);
  }

  private static EventStreamDocument Map(EventStreamDocumentEntity doc) => new()
  {
    Id = doc.DocumentId,
    StreamId = doc.StreamId,
    DocumentType = doc.DocumentType,
    Version = doc.Version,
    Time = doc.Time,
    Name = doc.Name,
    Data = doc.Data,
    DataType = doc.DataType,
    TargetType = doc.TargetType,
    MetaData = new()
    {
      UserId = doc.MetaData.UserId,
      UserName = doc.MetaData.UserName,
      TenantId = doc.MetaData.TenantId,
      Comment = doc.MetaData.Comment,
      Additional = doc.MetaData.Additional,
    },
  };

  public async Task AppendAsync<TEvent>(
    Guid id,
    TEvent evt,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default
  ) where TEvent : notnull
  {
    string eventName = _eventTypeProvider.ResolveType(typeof(TEvent));
    EventStreamDocumentEntity document = await CreateEventEntity(id, evt, metaData, eventName).ConfigureAwait(false);
    bool indexUpdateSuccessful = false;
    int retryCount = 0;
    do
    {
      try
      {
        ItemResponse<EventStreamIndexEntity>? indexPatch = await _dbProvider.Container
          .PatchItemAsync<EventStreamIndexEntity>(
            _stream.Id,
            new(StreamId.ToString()),
            new List<PatchOperation>()
            {
              PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.NextVersion), _stream.NextVersion + 1),
              PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.Version), _stream.NextVersion),
              PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.Updated), DateTimeOffset.Now),
            },
            new PatchItemRequestOptions() { IfMatchEtag = _stream.ETag },
            cancellationToken).ConfigureAwait(false);

        _stream = indexPatch.Resource;
        indexUpdateSuccessful = true;
      }
      catch (CosmosException e)
      {
        Logging.IndexPatchConcurrency(_logger, e, _stream.StreamId);
        retryCount++;
        await Task.Delay(TimeSpan.FromMilliseconds(9), cancellationToken).ConfigureAwait(false);
        await RefreshIndexAsync(cancellationToken).ConfigureAwait(false);
      }
    } while (!indexUpdateSuccessful && retryCount < _options.ConcurrencyRetryCount);

    Logging.AppendingEvent(_logger, document.DataType, document.StreamId, document.Version);
    var result = await _dbProvider.Container.CreateItemAsync(
        document,
        new(StreamId.ToString()),
        cancellationToken: cancellationToken)
      .ConfigureAwait(false);
  }

  private async Task RefreshIndexAsync(CancellationToken cancellationToken) => _stream = await _dbProvider.Container
      .ReadItemAsync<EventStreamIndexEntity>(_stream.Id, new(StreamId.ToString()), cancellationToken: cancellationToken)
      .ConfigureAwait(false);

  private async ValueTask<EventStreamDocumentEntity> CreateEventEntity<TEvent>(
    Guid id,
    TEvent evt,
    EventStreamMetaData? metaData,
    string eventName
  ) where TEvent : notnull => new()
  {
    Id = await _idStrategy.GenerateIdAsync(StreamId, _stream.NextVersion, EventStreamDocumentType.Event),
    DocumentId = id,
    StreamId = StreamId,
    Version = _stream.NextVersion,
    Data = JObject.FromObject(evt),
    DataType = eventName,
    Name = eventName,
    Time = DateTimeOffset.Now,
    DocumentType = EventStreamDocumentType.Event,
    MetaData = metaData ?? new(),
    TargetType = _stream.TargetType,
  };

  
  public Task<IEventStoreTransactionAppender> CreateTransactionalBatchAsync() =>
    Task.FromResult<IEventStoreTransactionAppender>(
      new CosmosEventStreamTransactionAppender(this));

  

  public IAsyncEnumerable<EventStreamDocument> ListAsync(
    ulong startVersion = 0u,
    CancellationToken cancellationToken = default
  ) => ListAsync(startVersion, _stream.Version, cancellationToken);

  public async IAsyncEnumerable<EventStreamDocument> ListAsync(
    ulong startVersion,
    ulong endVersion,
    [EnumeratorCancellation] CancellationToken cancellationToken = default
  )
  {
    FeedIterator<EventStreamDocument> iterator = _dbProvider.Container.GetItemLinqQueryable<EventStreamDocumentEntity>()
      .Where(doc => doc.StreamId == _stream.StreamId)
      .OrderBy(doc => doc.Version)
      .Select(doc => Map(doc))
      .ToFeedIterator();

    while (iterator.HasMoreResults && !cancellationToken.IsCancellationRequested)
    {
      FeedResponse<EventStreamDocument> batch = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
      foreach (EventStreamDocument doc in batch)
      {
        if (cancellationToken.IsCancellationRequested)
        {
          break;
        }
        yield return doc;
      }
    }
  }
  
  private class CosmosEventStreamTransactionAppender : IEventStoreTransactionAppender
  {
    private readonly CosmosEventStream _stream;

    private readonly List<EventStreamDocumentTemplate> _events = [];

    internal CosmosEventStreamTransactionAppender(CosmosEventStream stream)
    {
      _stream = stream;
    }

    public IEventStoreTransactionAppender Add<TEvent>(
      Guid id,
      TEvent evt,
      EventStreamMetaData? metaData = null,
      CancellationToken cancellationToken = default
    ) where TEvent : notnull
    {
      string eventName = _stream._eventTypeProvider.ResolveType(typeof(TEvent));
      _events.Add(new(
        id,
        JObject.FromObject(evt),
        eventName,
        eventName,
        DateTimeOffset.Now,
        metaData ?? new(),
        _stream._stream.TargetType
      ));

      return this;
    }
    
    private record EventStreamDocumentTemplate(
      Guid DocumentId,
      JObject Data,
      string DataType,
      string Name,
      DateTimeOffset Time,
      EventStreamMetaData MetaData,
      string TargetType
    );

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
      if (_events.Count == 0)
      {
        return;
      }

      ulong currentVersion = _stream.Version;

      try
      {
        bool indexUpdateSuccessful = false;
        int retryCount = 0;
        // update index
        do
        {
          try
          {
            ulong targetVersion = _stream.Version + (ulong)_events.Count;
            
            ItemResponse<EventStreamIndexEntity>? indexPatch = await _stream._dbProvider.Container
              .PatchItemAsync<EventStreamIndexEntity>(
                _stream._stream.Id,
                new(_stream.StreamId.ToString()),
                new List<PatchOperation>()
                {
                  PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.NextVersion), targetVersion + 1),
                  PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.Version), targetVersion),
                  PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.Updated), DateTimeOffset.Now),
                },
                new PatchItemRequestOptions() { IfMatchEtag = _stream._stream.ETag },
                cancellationToken).ConfigureAwait(false);

            _stream._stream = indexPatch.Resource;
            indexUpdateSuccessful = true;
          }
          catch (CosmosException e)
          {
            Logging.IndexPatchConcurrency(_stream._logger, e, _stream.StreamId);
            retryCount++;
            await Task.Delay(TimeSpan.FromMilliseconds(9), cancellationToken).ConfigureAwait(false);
            await _stream.RefreshIndexAsync(cancellationToken).ConfigureAwait(false);
            currentVersion = _stream.Version;
          }
        } while (!indexUpdateSuccessful && retryCount < _stream._options.ConcurrencyRetryCount);
        
        // store events
        var batch =
          _stream._dbProvider.Container.CreateTransactionalBatch(new PartitionKey(_stream.StreamId.ToString()));
        foreach (var evt in _events)
        {
          EventStreamDocumentEntity document = new()
          {
            Id = await _stream._idStrategy.GenerateIdAsync(_stream.StreamId, currentVersion,
              EventStreamDocumentType.Event),
            DocumentId = evt.DocumentId,
            StreamId = _stream.StreamId,
            Version = currentVersion,
            Data = evt.Data,
            DataType = evt.DataType,
            Name = evt.Name,
            TargetType = evt.TargetType,
            Time = evt.Time,
            DocumentType = EventStreamDocumentType.Event,
            MetaData = evt.MetaData, 
          };

          batch.CreateItem(document);
          
          currentVersion++;
        }

        await batch.ExecuteAsync(cancellationToken).ConfigureAwait(false);
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        throw;
      }
    }
  }
}
