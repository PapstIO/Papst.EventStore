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

  public Guid StreamId => _stream.StreamId;
  public ulong Version => _stream.Version;
  public DateTimeOffset Created => _stream.Created;

  public CosmosEventStream(
    ILogger<CosmosEventStream> logger,
    EventStreamIndexEntity stream,
    CosmosDatabaseProvider dbProvider,
    IEventTypeProvider eventTypeProvider,
    ICosmosIdStrategy idStrategy
  )
  {
    _logger = logger;
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

    Logging.AppendingEvent(_logger, document.DataType, document.StreamId, document.Version);
    await _dbProvider.Container.CreateItemAsync(
        document,
        new(StreamId.ToString()),
        cancellationToken: cancellationToken)
      .ConfigureAwait(false);

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
  }

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

  public Task<IEventStoreBatchAppender> AppendBatchAsync()
  {
    throw new NotImplementedException();
  }

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
}
