using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Papst.EventStore.AzureCosmos.Database;
using Papst.EventStore.Documents;
using Papst.EventStore.Exceptions;
using System.Runtime.CompilerServices;

namespace Papst.EventStore.AzureCosmos;

internal sealed class CosmosEventStream(
  ILogger<CosmosEventStream> logger,
  CosmosEventStoreOptions options,
  EventStreamIndexEntity stream,
  CosmosDatabaseProvider dbProvider,
  IEventTypeProvider eventTypeProvider,
  ICosmosIdStrategy idStrategy,
  TimeProvider timeProvider
)
  : IEventStream
{
  private const int MaxBatchSize = 100;

  private EventStreamIndexEntity _stream = stream;

  /// <inheritdoc />
  public Guid StreamId => _stream.StreamId;

  /// <inheritdoc />
  public ulong Version => _stream.Version;

  /// <inheritdoc />
  public DateTimeOffset Created => _stream.Created;

  /// <inheritdoc />
  public ulong? LatestSnapshotVersion => _stream.LatestSnapshotVersion;

  /// <inheritdoc />
  public EventStreamMetaData MetaData => _stream.MetaData;

  public async Task<EventStreamDocument?> GetLatestSnapshot(CancellationToken cancellationToken = default)
  {
    if (!_stream.LatestSnapshotVersion.HasValue)
    {
      return null;
    }

    string snapShotId = await idStrategy.GenerateIdAsync(
      _stream.StreamId,
      _stream.LatestSnapshotVersion.Value,
      EventStreamDocumentType.Snapshot).ConfigureAwait(false);

    ItemResponse<EventStreamDocumentEntity> result = await dbProvider.Container
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
    string eventName = eventTypeProvider.ResolveType(typeof(TEvent));
    EventStreamDocumentEntity document = await CreateEventEntity(id, evt, metaData, eventName).ConfigureAwait(false);
    bool indexUpdateSuccessful = false;
    int retryCount = 0;
    do
    {
      try
      {
        List<PatchOperation> patches =
        [
          PatchOperation.Set('/' + nameof(EventStreamIndexEntity.NextVersion), _stream.NextVersion + 1),
          PatchOperation.Set('/' + nameof(EventStreamIndexEntity.Version), _stream.NextVersion),
          PatchOperation.Set('/' + nameof(EventStreamIndexEntity.Updated), timeProvider.GetLocalNow()),
        ];
        if (metaData is not null && options.UpdateTenantIdOnAppend && metaData.TenantId is not null)
        {
          patches.Add(PatchOperation.Set(
            '/' + nameof(EventStreamIndexEntity.MetaData) + '/' + nameof(EventStreamMetaData.TenantId),
            metaData.TenantId)
          );
        }

        ItemResponse<EventStreamIndexEntity> indexPatch = await dbProvider.Container
          .PatchItemAsync<EventStreamIndexEntity>(
            _stream.Id,
            new(StreamId.ToString()),
            patches,
            new PatchItemRequestOptions() { IfMatchEtag = _stream.ETag },
            cancellationToken).ConfigureAwait(false);

        _stream = indexPatch.Resource;
        indexUpdateSuccessful = true;
      }
      catch (CosmosException e)
      {
        logger.IndexPatchConcurrency(e, _stream.StreamId);
        retryCount++;
        await Task.Delay(TimeSpan.FromMilliseconds(9), cancellationToken).ConfigureAwait(false);
        await RefreshIndexAsync(cancellationToken).ConfigureAwait(false);
      }
    } while (!indexUpdateSuccessful && retryCount < options.ConcurrencyRetryCount);

    logger.AppendingEvent(document.DataType, document.StreamId, document.Version);
    _ = await dbProvider.Container.CreateItemAsync(
        document,
        new(StreamId.ToString()),
        cancellationToken: cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task AppendSnapshotAsync<TEntity>(
    Guid id,
    TEntity entity,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default
  )
    where TEntity : notnull
  {
    string eventName = typeof(TEntity).Name;
    EventStreamDocumentEntity document =
      await CreateEventEntity(id, entity, metaData, eventName, EventStreamDocumentType.Snapshot).ConfigureAwait(false);
    bool indexUpdateSuccessful = false;
    int retryCount = 0;
    do
    {
      try
      {
        List<PatchOperation> patches =
        [
          PatchOperation.Set('/' + nameof(EventStreamIndexEntity.NextVersion), _stream.NextVersion + 1),
          PatchOperation.Set('/' + nameof(EventStreamIndexEntity.Version), _stream.NextVersion),
          PatchOperation.Set('/' + nameof(EventStreamIndexEntity.Updated), DateTimeOffset.Now),
          PatchOperation.Set('/' + nameof(EventStreamIndexEntity.LatestSnapshotVersion), _stream.NextVersion),
        ];
        if (metaData is not null && options.UpdateTenantIdOnAppend && metaData.TenantId is not null)
        {
          patches.Add(PatchOperation.Set(
            '/' + nameof(EventStreamIndexEntity.MetaData) + '/' + nameof(EventStreamMetaData.TenantId),
            metaData.TenantId)
          );
        }

        ItemResponse<EventStreamIndexEntity> indexPatch = await dbProvider.Container
          .PatchItemAsync<EventStreamIndexEntity>(
            _stream.Id,
            new PartitionKey(StreamId.ToString()),
            patches,
            new PatchItemRequestOptions() { IfMatchEtag = _stream.ETag },
            cancellationToken).ConfigureAwait(false);

        _stream = indexPatch.Resource;
        indexUpdateSuccessful = true;
      }
      catch (CosmosException e)
      {
        logger.IndexPatchConcurrency(e, _stream.StreamId);
        retryCount++;
        await Task.Delay(TimeSpan.FromMilliseconds(9), cancellationToken).ConfigureAwait(false);
        await RefreshIndexAsync(cancellationToken).ConfigureAwait(false);
      }
    } while (!indexUpdateSuccessful && retryCount < options.ConcurrencyRetryCount);

    logger.AppendingEvent(document.DataType, document.StreamId, document.Version);
    _ = await dbProvider.Container.CreateItemAsync(
        document,
        new PartitionKey(StreamId.ToString()),
        cancellationToken: cancellationToken)
      .ConfigureAwait(false);
  }

  private async Task RefreshIndexAsync(CancellationToken cancellationToken) => _stream = await dbProvider.Container
    .ReadItemAsync<EventStreamIndexEntity>(_stream.Id, new(StreamId.ToString()), cancellationToken: cancellationToken)
    .ConfigureAwait(false);

  private async ValueTask<EventStreamDocumentEntity> CreateEventEntity<TEvent>(
    Guid id,
    TEvent evt,
    EventStreamMetaData? metaData,
    string eventName,
    EventStreamDocumentType documentType = EventStreamDocumentType.Event
  ) where TEvent : notnull => new()
  {
    Id = await idStrategy.GenerateIdAsync(StreamId, _stream.NextVersion, documentType),
    DocumentId = id,
    StreamId = StreamId,
    Version = _stream.NextVersion,
    Data = JObject.FromObject(evt),
    DataType = eventName,
    Name = eventName,
    Time = timeProvider.GetLocalNow(),
    DocumentType = documentType,
    MetaData = metaData ?? new(),
    TargetType = _stream.TargetType,
  };


  public Task<IEventStoreTransactionAppender> CreateTransactionalBatchAsync() =>
    Task.FromResult<IEventStoreTransactionAppender>(
      new CosmosEventStreamTransactionAppender(this, timeProvider, eventTypeProvider, _stream.TargetType));


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
    FeedIterator<EventStreamDocumentEntity> iterator = dbProvider.Container
      .GetItemLinqQueryable<EventStreamDocumentEntity>()
      .Where(doc =>
        doc.StreamId == _stream.StreamId
        && doc.DocumentType != EventStreamDocumentType.Index
        && doc.Version >= startVersion
        && doc.Version <= endVersion)
      .OrderBy(doc => doc.Version)
      .ToFeedIterator();

    while (iterator.HasMoreResults && !cancellationToken.IsCancellationRequested)
    {
      FeedResponse<EventStreamDocumentEntity> batch =
        await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
      foreach (EventStreamDocumentEntity doc in batch)
      {
        if (cancellationToken.IsCancellationRequested)
        {
          break;
        }

        yield return Map(doc);
      }
    }
  }

  public async IAsyncEnumerable<EventStreamDocument> ListDescendingAsync(ulong endVersion,
    ulong startVersion,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    FeedIterator<EventStreamDocumentEntity> iterator = dbProvider.Container
      .GetItemLinqQueryable<EventStreamDocumentEntity>()
      .Where(doc =>
        doc.StreamId == _stream.StreamId
        && doc.DocumentType != EventStreamDocumentType.Index
        && doc.Version >= startVersion
        && doc.Version <= endVersion)
      .OrderByDescending(doc => doc.Version)
      .ToFeedIterator();

    while (iterator.HasMoreResults && !cancellationToken.IsCancellationRequested)
    {
      FeedResponse<EventStreamDocumentEntity> batch =
        await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
      foreach (EventStreamDocumentEntity doc in batch)
      {
        if (cancellationToken.IsCancellationRequested)
        {
          break;
        }

        yield return Map(doc);
      }
    }
  }

  public IAsyncEnumerable<EventStreamDocument> ListDescendingAsync(ulong endVersion,
    CancellationToken cancellationToken = default)
    => ListDescendingAsync(endVersion, 0u, cancellationToken);

  private class CosmosEventStreamTransactionAppender : IEventStoreTransactionAppender
  {
    private readonly CosmosEventStream _stream;

    private readonly List<EventStreamDocumentTemplate> _events = [];
    private readonly IEventTypeProvider _eventTypeProvider;
    private readonly TimeProvider _timeProvider;
    private readonly string _targetType;

    internal CosmosEventStreamTransactionAppender(CosmosEventStream stream,
      TimeProvider timeProvider,
      IEventTypeProvider eventTypeProvider,
      string targetType)
    {
      _stream = stream;
      _timeProvider = timeProvider;
      _eventTypeProvider = eventTypeProvider;
      _targetType = targetType;
    }

    public IEventStoreTransactionAppender Add<TEvent>(
      Guid id,
      TEvent evt,
      EventStreamMetaData? metaData = null,
      CancellationToken cancellationToken = default
    ) where TEvent : notnull
    {
      string eventName = _eventTypeProvider.ResolveType(typeof(TEvent));
      _events.Add(new(
        id,
        JObject.FromObject(evt),
        eventName,
        eventName,
        _timeProvider.GetLocalNow(),
        metaData ?? new(),
        _targetType
      ));

      return this;
    }


    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
      if (_events.Count == 0)
      {
        return;
      }

      await _stream.CommitTransactionAsync(_events, cancellationToken).ConfigureAwait(false);
    }
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

  private async Task CommitTransactionAsync(List<EventStreamDocumentTemplate> events,
    CancellationToken cancellationToken)
  {
    try
    {
      bool indexUpdateSuccessful = false;
      int retryCount = 0;
      var baseVersion = _stream.Version;
      // update index
      do
      {
        try
        {
          ulong targetVersion = baseVersion + (ulong)events.Count;

          ItemResponse<EventStreamIndexEntity>? indexPatch = await dbProvider.Container
            .PatchItemAsync<EventStreamIndexEntity>(
              _stream.Id,
              new(StreamId.ToString()),
              new List<PatchOperation>()
              {
                PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.NextVersion), targetVersion + 1),
                PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.Version), targetVersion),
                PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.Updated), timeProvider.GetLocalNow()),
              },
              new PatchItemRequestOptions() { IfMatchEtag = _stream.ETag },
              cancellationToken).ConfigureAwait(false);

          _stream = indexPatch.Resource;
          indexUpdateSuccessful = true;
        }
        catch (CosmosException e)
        {
          logger.IndexPatchConcurrency(e, StreamId);
          retryCount++;
          await Task.Delay(TimeSpan.FromMilliseconds(9), cancellationToken).ConfigureAwait(false);
          await RefreshIndexAsync(cancellationToken).ConfigureAwait(false);

          baseVersion = _stream.Version;
        }
      } while (!indexUpdateSuccessful && retryCount < options.ConcurrencyRetryCount);

      if (!indexUpdateSuccessful)
      {
        throw new EventStreamException(StreamId, "Failed to update Index");
      }

      var totalOps = 0;
      // Index updated, now commit events in batches MaxBatchSize at a time. Don't respect cancellation here, we want to finish the transaction
      // since we already updated the index
      await foreach (var batch in CreateBatches(events, MaxBatchSize, baseVersion)
                       .WithCancellation(CancellationToken.None))
      {
        TransactionalBatchResponse result = await batch.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        if (!result.IsSuccessStatusCode)
        {
          var firstFailed = result.FirstOrDefault(itm => !itm.IsSuccessStatusCode);
          var message = firstFailed is null
            ? $"Failed to commit events {string.Join(", ", result.Select(itm => $"{itm.StatusCode}"))} ActivityId: {result.ActivityId}"
            : $"Failed to commit event at op. Status={firstFailed.StatusCode}, ActivityId: {result.ActivityId}";
          throw new EventStreamException(StreamId, message);
        }

        totalOps += result.Count;
      }

      logger.TransactionCompleted(StreamId, totalOps);
    }
    catch (Exception e)
    {
      logger.TransactionException(e, StreamId);
      throw new EventStreamException(StreamId, "Exception during Transaction", e);
    }
  }

  private async IAsyncEnumerable<TransactionalBatch> CreateBatches(IReadOnlyList<EventStreamDocumentTemplate> events,
    int batchSize,
    ulong currentVersion = 0)
  {
    var currentBatch = dbProvider.Container.CreateTransactionalBatch(new PartitionKey(StreamId.ToString()));

    for (int i = 0; i < events.Count; i++)
    {
      var version = currentVersion + (ulong)i + 1;

      currentBatch.CreateItem(new EventStreamDocumentEntity
      {
        Id = await idStrategy.GenerateIdAsync(StreamId, version, EventStreamDocumentType.Event),
        DocumentId = events[i].DocumentId,
        StreamId = StreamId,
        Version = version,
        Data = events[i].Data,
        DataType = events[i].DataType,
        Name = events[i].Name,
        TargetType = events[i].TargetType,
        Time = events[i].Time,
        DocumentType = EventStreamDocumentType.Event,
        MetaData = events[i].MetaData
      });

      if ((i + 1) % batchSize != 0)
      {
        continue;
      }

      yield return currentBatch;
      currentBatch = dbProvider.Container.CreateTransactionalBatch(new PartitionKey(StreamId.ToString()));
    }

    if (events.Count % batchSize != 0)
    {
      yield return currentBatch;
    }
  }
}
