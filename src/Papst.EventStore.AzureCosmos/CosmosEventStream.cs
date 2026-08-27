using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Papst.EventStore.AzureCosmos.Database;
using Papst.EventStore.Documents;
using Papst.EventStore.Exceptions;
using Papst.EventStore.Pipeline;
using System.Runtime.CompilerServices;

namespace Papst.EventStore.AzureCosmos;

internal sealed class CosmosEventStream(
  ILogger<CosmosEventStream> logger,
  CosmosEventStoreOptions options,
  EventStreamIndexEntity stream,
  CosmosDatabaseProvider dbProvider,
  IEventTypeProvider eventTypeProvider,
  ICosmosIdStrategy idStrategy,
  TimeProvider timeProvider,
  IEventStorePipeline<EventAppendContext> pipeline
)
  : IEventStream, ILowLevelEventStream
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
    Signature = doc.Signature,
    MetaData = new()
    {
      UserId = doc.MetaData.UserId,
      UserName = doc.MetaData.UserName,
      TenantId = doc.MetaData.TenantId,
      Comment = doc.MetaData.Comment,
      Additional = doc.MetaData.Additional,
    },
  };

  public Task AppendAsync(Guid id, string eventType, JObject evt, EventStreamMetaData? metaData = null, CancellationToken cancellationToken = default)
  {
    EventStreamDocument document = BuildDocument(id, evt, metaData, eventType, EventStreamDocumentType.Event);
    return AppendThroughPipelineAsync(document, EventStreamDocumentType.Event, metaData, cancellationToken);
  }

  public Task AppendAsync<TEvent>(
    Guid id,
    TEvent evt,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default
  ) where TEvent : notnull
  {
    string eventName = eventTypeProvider.ResolveType(typeof(TEvent));
    EventStreamDocument document = BuildDocument(id, JObject.FromObject(evt), metaData, eventName, EventStreamDocumentType.Event);
    return AppendThroughPipelineAsync(document, EventStreamDocumentType.Event, metaData, cancellationToken);
  }

  public Task AppendSnapshotAsync<TEntity>(
    Guid id,
    TEntity entity,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default
  )
    where TEntity : notnull
  {
    string eventName = typeof(TEntity).Name;
    EventStreamDocument document = BuildDocument(id, JObject.FromObject(entity), metaData, eventName, EventStreamDocumentType.Snapshot);
    return AppendThroughPipelineAsync(document, EventStreamDocumentType.Snapshot, metaData, cancellationToken);
  }

  // Builds the storage-agnostic core document the append pipeline operates on.
  private EventStreamDocument BuildDocument(Guid id, JObject data, EventStreamMetaData? metaData, string eventName, EventStreamDocumentType documentType)
    => new()
    {
      Id = id,
      StreamId = StreamId,
      Version = _stream.NextVersion,
      Data = data,
      DataType = eventName,
      Name = eventName,
      Time = timeProvider.GetLocalNow(),
      DocumentType = documentType,
      MetaData = metaData ?? new(),
      TargetType = _stream.TargetType,
    };

  private async ValueTask<EventStreamDocumentEntity> MapToEntityAsync(EventStreamDocument doc)
    => new()
    {
      Id = await idStrategy.GenerateIdAsync(StreamId, doc.Version, doc.DocumentType).ConfigureAwait(false),
      DocumentId = doc.Id,
      StreamId = StreamId,
      Version = doc.Version,
      Data = doc.Data,
      DataType = doc.DataType,
      Name = doc.Name,
      Time = doc.Time,
      DocumentType = doc.DocumentType,
      MetaData = doc.MetaData,
      TargetType = doc.TargetType,
      Signature = doc.Signature,
    };

  private async Task AppendThroughPipelineAsync(
    EventStreamDocument document,
    EventStreamDocumentType documentType,
    EventStreamMetaData? metaData,
    CancellationToken cancellationToken)
  {
    var context = new EventAppendContext
    {
      StreamId = StreamId,
      DocumentType = documentType,
      MetaData = metaData,
      PreviousSignature = _stream.LatestSignature,
      Entries = [new EventAppendEntry(document)],
    };

    await pipeline.ExecuteAsync(context, async () =>
    {
      EventStreamDocument signed = context.Entries[0].Document;
      EventStreamDocumentEntity entity = await MapToEntityAsync(signed).ConfigureAwait(false);

      List<PatchOperation> extraPatches = SignaturePatches(signed.Signature);
      if (documentType == EventStreamDocumentType.Snapshot)
      {
        extraPatches.Add(PatchOperation.Set('/' + nameof(EventStreamIndexEntity.LatestSnapshotVersion), signed.Version));
      }

      await AppendDocumentAsync(entity, metaData, cancellationToken, extraPatches.ToArray()).ConfigureAwait(false);
    }, cancellationToken).ConfigureAwait(false);
  }

  private static List<PatchOperation> SignaturePatches(EventSignature? signature)
  {
    if (signature is null)
    {
      return [];
    }

    return
    [
      PatchOperation.Set('/' + nameof(EventStreamIndexEntity.LatestSignature), signature.Value),
      PatchOperation.Set('/' + nameof(EventStreamIndexEntity.SigningAlgorithm), signature.Algorithm),
      PatchOperation.Set('/' + nameof(EventStreamIndexEntity.SigningCertificateThumbprint), signature.CertificateThumbprint),
    ];
  }

  private async Task RefreshIndexAsync(CancellationToken cancellationToken) => _stream = await dbProvider.Container
    .ReadItemAsync<EventStreamIndexEntity>(_stream.Id, new(StreamId.ToString()), cancellationToken: cancellationToken)
    .ConfigureAwait(false);

  private async Task AppendDocumentAsync(
    EventStreamDocumentEntity document,
    EventStreamMetaData? metaData,
    CancellationToken cancellationToken,
    params PatchOperation[] additionalPatches
  )
  {
    bool indexUpdateSuccessful = false;
    int retryCount = 0;
    do
    {
      try
      {
        List<PatchOperation> patches = CreateAppendPatches(metaData, additionalPatches);

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

  private List<PatchOperation> CreateAppendPatches(
    EventStreamMetaData? metaData,
    params PatchOperation[] additionalPatches
  )
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

    patches.AddRange(additionalPatches);

    return patches;
  }

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

  public async Task UpdateStreamMetaData(EventStreamMetaData metaData, CancellationToken cancellationToken = default)
  {
    bool indexUpdateSuccessful = false;
    int retryCount = 0;
    do
    {
      try
      {
        List<PatchOperation> patches = [];
        if (metaData.UserId != _stream.MetaData.UserId)
        {
          patches.Add(PatchOperation.Set(
            '/' + nameof(EventStreamIndexEntity.MetaData) + '/' + nameof(EventStreamMetaData.UserId),
            metaData.UserId
          ));
        }

        if (metaData.UserName != _stream.MetaData.UserName)
        {
          patches.Add(PatchOperation.Set(
            '/' + nameof(EventStreamIndexEntity.MetaData) + '/' + nameof(EventStreamMetaData.UserName),
            metaData.UserName));
        }

        if (metaData.TenantId != _stream.MetaData.TenantId)
        {
          patches.Add(PatchOperation.Set(
            '/' + nameof(EventStreamIndexEntity.MetaData) + '/' + nameof(EventStreamMetaData.TenantId),
            metaData.TenantId
          ));
        }

        if (metaData.Comment != _stream.MetaData.Comment)
        {
          patches.Add(PatchOperation.Set(
            '/' + nameof(EventStreamIndexEntity.MetaData) + '/' + nameof(EventStreamMetaData.Comment),
            metaData.Comment
          ));
        }

        if (metaData.Additional is not null)
        {
          if (_stream.MetaData.Additional is null)
          {
            patches.Add(PatchOperation.Add(
              '/' + nameof(EventStreamIndexEntity.MetaData) + '/' + nameof(EventStreamMetaData.Additional),
              metaData.Additional)
            );
          }
          else
          {
            patches.Add(PatchOperation.Replace(
              '/' + nameof(EventStreamIndexEntity.MetaData) + '/' + nameof(EventStreamMetaData.Additional),
              metaData.Additional)
            );
          }
        }
        else if (_stream.MetaData is not null)
        {
          patches.Add(PatchOperation.Remove('/' + nameof(EventStreamIndexEntity.MetaData) + '/' + nameof(EventStreamMetaData.Additional)));
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
  }

  private sealed class CosmosEventStreamTransactionAppender : IEventStoreTransactionAppender
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
      List<EventStreamDocumentEntity> entities = [];
      // update index
      do
      {
        try
        {
          // Sign the batch for the current version window and chain head, so the
          // reserved versions and the new chain head are written in one index patch.
          entities = await BuildAndSignBatchAsync(events, baseVersion, cancellationToken).ConfigureAwait(false);
          EventSignature? lastSignature = entities.Count > 0 ? entities[^1].Signature : null;
          ulong targetVersion = baseVersion + (ulong)events.Count;

          List<PatchOperation> patches =
          [
            PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.NextVersion), targetVersion + 1),
            PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.Version), targetVersion),
            PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.Updated), timeProvider.GetLocalNow()),
          ];
          patches.AddRange(SignaturePatches(lastSignature));

          ItemResponse<EventStreamIndexEntity>? indexPatch = await dbProvider.Container
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
      await foreach (var batch in CreateBatches(entities, MaxBatchSize)
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

  // Builds core documents for the reserved version window, runs them through the
  // append pipeline (which may sign them, chaining onto the current head), and maps
  // them to persistable entities. Re-run on each concurrency retry so the versions
  // and chain head stay correct.
  private async Task<List<EventStreamDocumentEntity>> BuildAndSignBatchAsync(
    IReadOnlyList<EventStreamDocumentTemplate> events,
    ulong baseVersion,
    CancellationToken cancellationToken)
  {
    var coreDocuments = new List<EventAppendEntry>(events.Count);
    for (int i = 0; i < events.Count; i++)
    {
      ulong version = baseVersion + (ulong)i + 1;
      coreDocuments.Add(new EventAppendEntry(new EventStreamDocument
      {
        Id = events[i].DocumentId,
        StreamId = StreamId,
        Version = version,
        Data = events[i].Data,
        DataType = events[i].DataType,
        Name = events[i].Name,
        Time = events[i].Time,
        DocumentType = EventStreamDocumentType.Event,
        MetaData = events[i].MetaData,
        TargetType = events[i].TargetType,
      }));
    }

    var context = new EventAppendContext
    {
      StreamId = StreamId,
      DocumentType = EventStreamDocumentType.Event,
      MetaData = null,
      PreviousSignature = _stream.LatestSignature,
      Entries = coreDocuments,
    };

    await pipeline.ExecuteAsync(context, () => Task.CompletedTask, cancellationToken).ConfigureAwait(false);

    var entities = new List<EventStreamDocumentEntity>(events.Count);
    foreach (EventAppendEntry entry in context.Entries)
    {
      entities.Add(await MapToEntityAsync(entry.Document).ConfigureAwait(false));
    }

    return entities;
  }

  private async IAsyncEnumerable<TransactionalBatch> CreateBatches(IReadOnlyList<EventStreamDocumentEntity> entities,
    int batchSize)
  {
    var currentBatch = dbProvider.Container.CreateTransactionalBatch(new PartitionKey(StreamId.ToString()));

    for (int i = 0; i < entities.Count; i++)
    {
      currentBatch = currentBatch.CreateItem(entities[i]);

      if ((i + 1) % batchSize != 0)
      {
        continue;
      }

      yield return currentBatch;
      currentBatch = dbProvider.Container.CreateTransactionalBatch(new PartitionKey(StreamId.ToString()));
    }

    if (entities.Count % batchSize != 0)
    {
      yield return currentBatch;
    }

    await Task.CompletedTask;
  }

}
