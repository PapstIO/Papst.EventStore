﻿using System.Runtime.CompilerServices;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Papst.EventStore.AzureCosmos.Database;
using Papst.EventStore.Documents;

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

  public Guid StreamId => stream.StreamId;
  public ulong Version => stream.Version;
  public DateTimeOffset Created => stream.Created;
  public ulong? LatestSnapshotVersion => stream.LatestSnapshotVersion;


  public async Task<EventStreamDocument?> GetLatestSnapshot(CancellationToken cancellationToken = default)
  {
    if (!stream.LatestSnapshotVersion.HasValue)
    {
      return null;
    }

    string snapShotId = await idStrategy.GenerateIdAsync(
      stream.StreamId,
      stream.LatestSnapshotVersion.Value,
      EventStreamDocumentType.Snapshot).ConfigureAwait(false);

    ItemResponse<EventStreamDocumentEntity> result = await dbProvider.Container
      .ReadItemAsync<EventStreamDocumentEntity>(
        snapShotId,
        new(stream.StreamId.ToString()),
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
        ItemResponse<EventStreamIndexEntity>? indexPatch = await dbProvider.Container
          .PatchItemAsync<EventStreamIndexEntity>(
            stream.Id,
            new(StreamId.ToString()),
            [
              PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.NextVersion), stream.NextVersion + 1),
              PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.Version), stream.NextVersion),
              PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.Updated), timeProvider.GetLocalNow()),
            ],
            new PatchItemRequestOptions() { IfMatchEtag = stream.ETag },
            cancellationToken).ConfigureAwait(false);

        stream = indexPatch.Resource;
        indexUpdateSuccessful = true;
      }
      catch (CosmosException e)
      {
        Logging.IndexPatchConcurrency(logger, e, stream.StreamId);
        retryCount++;
        await Task.Delay(TimeSpan.FromMilliseconds(9), cancellationToken).ConfigureAwait(false);
        await RefreshIndexAsync(cancellationToken).ConfigureAwait(false);
      }
    } while (!indexUpdateSuccessful && retryCount < options.ConcurrencyRetryCount);

    Logging.AppendingEvent(logger, document.DataType, document.StreamId, document.Version);
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
    EventStreamDocumentEntity document = await CreateEventEntity(id, entity, metaData, eventName, EventStreamDocumentType.Snapshot).ConfigureAwait(false);
    bool indexUpdateSuccessful = false;
    int retryCount = 0;
    do
    {
      try
      {
        ItemResponse<EventStreamIndexEntity>? indexPatch = await dbProvider.Container
          .PatchItemAsync<EventStreamIndexEntity>(
            stream.Id,
            new PartitionKey(StreamId.ToString()),
            [
              PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.NextVersion), stream.NextVersion + 1),
              PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.Version), stream.NextVersion),
              PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.Updated), DateTimeOffset.Now),
              PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.LatestSnapshotVersion), stream.NextVersion), 
            ],
            new PatchItemRequestOptions() { IfMatchEtag = stream.ETag },
            cancellationToken).ConfigureAwait(false);

        stream = indexPatch.Resource;
        indexUpdateSuccessful = true;
      }
      catch (CosmosException e)
      {
        Logging.IndexPatchConcurrency(logger, e, stream.StreamId);
        retryCount++;
        await Task.Delay(TimeSpan.FromMilliseconds(9), cancellationToken).ConfigureAwait(false);
        await RefreshIndexAsync(cancellationToken).ConfigureAwait(false);
      }
    } while (!indexUpdateSuccessful && retryCount < options.ConcurrencyRetryCount);

    Logging.AppendingEvent(logger, document.DataType, document.StreamId, document.Version);
    _ = await dbProvider.Container.CreateItemAsync(
        document,
        new(StreamId.ToString()),
        cancellationToken: cancellationToken)
      .ConfigureAwait(false);
  }

  private async Task RefreshIndexAsync(CancellationToken cancellationToken) => stream = await dbProvider.Container
    .ReadItemAsync<EventStreamIndexEntity>(stream.Id, new(StreamId.ToString()), cancellationToken: cancellationToken)
    .ConfigureAwait(false);

  private async ValueTask<EventStreamDocumentEntity> CreateEventEntity<TEvent>(
    Guid id,
    TEvent evt,
    EventStreamMetaData? metaData,
    string eventName,
    EventStreamDocumentType documentType = EventStreamDocumentType.Event
  ) where TEvent : notnull => new()
  {
    Id = await idStrategy.GenerateIdAsync(StreamId, stream.NextVersion, documentType),
    DocumentId = id,
    StreamId = StreamId,
    Version = stream.NextVersion,
    Data = JObject.FromObject(evt),
    DataType = eventName,
    Name = eventName,
    Time = timeProvider.GetLocalNow(),
    DocumentType = documentType,
    MetaData = metaData ?? new(),
    TargetType = stream.TargetType,
  };


  public Task<IEventStoreTransactionAppender> CreateTransactionalBatchAsync() =>
    Task.FromResult<IEventStoreTransactionAppender>(new CosmosEventStreamTransactionAppender(this, timeProvider, eventTypeProvider, stream.TargetType));


  public IAsyncEnumerable<EventStreamDocument> ListAsync(
    ulong startVersion = 0u,
    CancellationToken cancellationToken = default
  ) => ListAsync(startVersion, stream.Version, cancellationToken);

  public async IAsyncEnumerable<EventStreamDocument> ListAsync(
    ulong startVersion,
    ulong endVersion,
    [EnumeratorCancellation] CancellationToken cancellationToken = default
  )
  {
    FeedIterator<EventStreamDocument> iterator = dbProvider.Container.GetItemLinqQueryable<EventStreamDocumentEntity>()
      .Where(doc => doc.StreamId == stream.StreamId)
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
    private readonly IEventTypeProvider _eventTypeProvider;
    private readonly TimeProvider _timeProvider;
    private readonly string _targetType;

    internal CosmosEventStreamTransactionAppender(CosmosEventStream stream, TimeProvider timeProvider, IEventTypeProvider eventTypeProvider, string targetType)
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

  private async Task CommitTransactionAsync(List<EventStreamDocumentTemplate> events, CancellationToken cancellationToken)
  {
    ulong currentVersion = stream.Version;

      try
      {
        bool indexUpdateSuccessful = false;
        int retryCount = 0;
        // update index
        do
        {
          try
          {
            ulong targetVersion = stream.Version + (ulong)events.Count;

            ItemResponse<EventStreamIndexEntity>? indexPatch = await dbProvider.Container
              .PatchItemAsync<EventStreamIndexEntity>(
                stream.Id,
                new(StreamId.ToString()),
                new List<PatchOperation>()
                {
                  PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.NextVersion), targetVersion + 1),
                  PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.Version), targetVersion),
                  PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.Updated), timeProvider.GetLocalNow()),
                },
                new PatchItemRequestOptions() { IfMatchEtag = stream.ETag },
                cancellationToken).ConfigureAwait(false);

            stream = indexPatch.Resource;
            indexUpdateSuccessful = true;
          }
          catch (CosmosException e)
          {
            Logging.IndexPatchConcurrency(logger, e, StreamId);
            retryCount++;
            await Task.Delay(TimeSpan.FromMilliseconds(9), cancellationToken).ConfigureAwait(false);
            await RefreshIndexAsync(cancellationToken).ConfigureAwait(false);
            currentVersion = stream.Version;
          }
        } while (!indexUpdateSuccessful && retryCount < options.ConcurrencyRetryCount);

        // store events
        var batch =
          dbProvider.Container.CreateTransactionalBatch(new PartitionKey(StreamId.ToString()));
        foreach (var evt in events)
        {
          EventStreamDocumentEntity document = new()
          {
            Id = await idStrategy.GenerateIdAsync(StreamId, currentVersion, EventStreamDocumentType.Event),
            DocumentId = evt.DocumentId,
            StreamId = StreamId,
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
