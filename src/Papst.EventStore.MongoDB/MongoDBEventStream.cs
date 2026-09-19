using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Papst.EventStore.Documents;
using Papst.EventStore.Pipeline;

namespace Papst.EventStore.MongoDB;

internal class MongoDBEventStream : IEventStream, ILowLevelEventStream
{
  private readonly IMongoCollection<EventStreamDocument> _documentsCollection;
  private readonly IMongoCollection<MongoEventStreamMetadata> _metadataCollection;
  private readonly TimeProvider _timeProvider;
  private readonly string _targetType;
  private readonly IEventTypeProvider _typeProvider;
  private readonly IEventStorePipeline<EventAppendContext> _pipeline;
  private readonly ILogger<MongoDBEventStream> _logger;
  private string? _latestSignature;

  public MongoDBEventStream(
    Guid streamId,
    ulong version,
    ulong nextVersion,
    DateTimeOffset created,
    EventStreamMetaData metaData,
    string targetType,
    TimeProvider timeProvider,
    IEventTypeProvider typeProvider,
    IMongoCollection<EventStreamDocument> documentsCollection,
    IMongoCollection<MongoEventStreamMetadata> metadataCollection,
    IEventStorePipeline<EventAppendContext> pipeline,
    ILogger<MongoDBEventStream> logger,
    string? latestSignature = null)
  {
    StreamId = streamId;
    Version = version;
    NextVersion = nextVersion;
    Created = created;
    MetaData = metaData;
    _targetType = targetType;
    _timeProvider = timeProvider;
    _typeProvider = typeProvider;
    _documentsCollection = documentsCollection;
    _metadataCollection = metadataCollection;
    _pipeline = pipeline;
    _logger = logger;
    _latestSignature = latestSignature;
  }

  public Guid StreamId { get; }
  public ulong Version { get; private set; }

  // Version to assign to the next appended event. Tracked alongside Version so the first
  // event of a freshly created stream is version 0 (an empty stream also reports Version 0).
  private ulong NextVersion { get; set; }

  public DateTimeOffset Created { get; }
  public EventStreamMetaData MetaData { get; private set; }

  public ulong? LatestSnapshotVersion
  {
    get
    {
      var filter = Builders<EventStreamDocument>.Filter.And(
        Builders<EventStreamDocument>.Filter.Eq(d => d.StreamId, StreamId),
        Builders<EventStreamDocument>.Filter.Eq(d => d.DocumentType, EventStreamDocumentType.Snapshot)
      );
      var sort = Builders<EventStreamDocument>.Sort.Descending(d => d.Version);
      var snapshot = _documentsCollection.Find(filter).Sort(sort).FirstOrDefault();
      return snapshot?.Version;
    }
  }

  public async Task<EventStreamDocument?> GetLatestSnapshot(CancellationToken cancellationToken = default)
  {
    var filter = Builders<EventStreamDocument>.Filter.And(
      Builders<EventStreamDocument>.Filter.Eq(d => d.StreamId, StreamId),
      Builders<EventStreamDocument>.Filter.Eq(d => d.DocumentType, EventStreamDocumentType.Snapshot)
    );
    var sort = Builders<EventStreamDocument>.Sort.Descending(d => d.Version);
    return await _documentsCollection.Find(filter).Sort(sort).FirstOrDefaultAsync(cancellationToken);
  }

  public Task AppendAsync<TEvent>(
    Guid id,
    TEvent evt,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default) where TEvent : notnull
  {
    string name = _typeProvider.ResolveType(typeof(TEvent));
    var document = new EventStreamDocument
    {
      Id = id,
      StreamId = StreamId,
      Version = NextVersion,
      Time = _timeProvider.GetLocalNow(),
      DataType = name,
      Data = JObject.FromObject(evt),
      DocumentType = EventStreamDocumentType.Event,
      MetaData = metaData ?? new EventStreamMetaData(),
      TargetType = _targetType,
      Name = name
    };

    return AppendThroughPipelineAsync(document, EventStreamDocumentType.Event, metaData, cancellationToken);
  }

  public Task AppendAsync(
    Guid id,
    string eventType,
    JObject evt,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default)
  {
    var document = new EventStreamDocument
    {
      Id = id,
      StreamId = StreamId,
      Version = NextVersion,
      Time = _timeProvider.GetLocalNow(),
      DataType = eventType,
      Data = evt,
      DocumentType = EventStreamDocumentType.Event,
      MetaData = metaData ?? new EventStreamMetaData(),
      TargetType = _targetType,
      Name = eventType
    };

    return AppendThroughPipelineAsync(document, EventStreamDocumentType.Event, metaData, cancellationToken);
  }

  public Task AppendSnapshotAsync<TEntity>(
    Guid id,
    TEntity entity,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default) where TEntity : notnull
  {
    var document = new EventStreamDocument
    {
      Id = id,
      StreamId = StreamId,
      Version = NextVersion,
      Time = _timeProvider.GetLocalNow(),
      DataType = _targetType,
      Data = JObject.FromObject(entity),
      DocumentType = EventStreamDocumentType.Snapshot,
      MetaData = metaData ?? new EventStreamMetaData(),
      TargetType = _targetType,
      Name = _targetType
    };

    return AppendThroughPipelineAsync(document, EventStreamDocumentType.Snapshot, metaData, cancellationToken);
  }

  private async Task AppendThroughPipelineAsync(
    EventStreamDocument document,
    EventStreamDocumentType documentType,
    EventStreamMetaData? metaData,
    CancellationToken cancellationToken)
  {
    _logger.AppendingEvent(document.DataType, StreamId, document.Version);

    var context = new EventAppendContext
    {
      StreamId = StreamId,
      DocumentType = documentType,
      MetaData = metaData,
      PreviousSignature = _latestSignature,
      Entries = [new EventAppendEntry(document)],
    };

    await _pipeline.ExecuteAsync(context, async () =>
    {
      EventStreamDocument persisted = context.Entries[0].Document;
      ulong newVersion = persisted.Version;

      await _documentsCollection.InsertOneAsync(persisted, new InsertOneOptions(), cancellationToken).ConfigureAwait(false);

      var filter = Builders<MongoEventStreamMetadata>.Filter.Eq(m => m.StreamId, StreamId);
      var update = Builders<MongoEventStreamMetadata>.Update
        .Set(m => m.Version, newVersion)
        .Set(m => m.NextVersion, newVersion + 1)
        .Set(m => m.LatestSignature, persisted.Signature != null ? persisted.Signature.Value : _latestSignature)
        .Set(m => m.SigningAlgorithm, persisted.Signature != null ? persisted.Signature.Algorithm : null)
        .Set(m => m.SigningCertificateThumbprint, persisted.Signature != null ? persisted.Signature.CertificateThumbprint : null);
      if (documentType == EventStreamDocumentType.Snapshot)
      {
        update = update.Set(m => m.LatestSnapshotVersion, newVersion);
      }

      await _metadataCollection.UpdateOneAsync(filter, update, new UpdateOptions(), cancellationToken).ConfigureAwait(false);

      Version = newVersion;
      NextVersion = newVersion + 1;
      if (persisted.Signature is not null)
      {
        _latestSignature = persisted.Signature.Value;
      }
    }, cancellationToken).ConfigureAwait(false);
  }

  public Task<IEventStoreTransactionAppender> CreateTransactionalBatchAsync()
  {
    return Task.FromResult<IEventStoreTransactionAppender>(
      new MongoDBTransactionalBatch(
        _documentsCollection,
        _metadataCollection,
        _typeProvider,
        _timeProvider,
        StreamId,
        _targetType,
        _pipeline,
        _logger
      )
    );
  }

  public async IAsyncEnumerable<EventStreamDocument> ListAsync(
    ulong startVersion = 0,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    var filter = Builders<EventStreamDocument>.Filter.And(
      Builders<EventStreamDocument>.Filter.Eq(d => d.StreamId, StreamId),
      Builders<EventStreamDocument>.Filter.Gte(d => d.Version, startVersion)
    );
    var sort = Builders<EventStreamDocument>.Sort.Ascending(d => d.Version);

    using var cursor = await _documentsCollection.FindAsync(filter, new FindOptions<EventStreamDocument>
    {
      Sort = sort
    }, cancellationToken);

    while (await cursor.MoveNextAsync(cancellationToken))
    {
      foreach (var document in cursor.Current)
      {
        yield return document;
      }
    }
  }

  public async IAsyncEnumerable<EventStreamDocument> ListAsync(
    ulong startVersion,
    ulong endVersion,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    var filter = Builders<EventStreamDocument>.Filter.And(
      Builders<EventStreamDocument>.Filter.Eq(d => d.StreamId, StreamId),
      Builders<EventStreamDocument>.Filter.Gte(d => d.Version, startVersion),
      Builders<EventStreamDocument>.Filter.Lte(d => d.Version, endVersion)
    );
    var sort = Builders<EventStreamDocument>.Sort.Ascending(d => d.Version);

    using var cursor = await _documentsCollection.FindAsync(filter, new FindOptions<EventStreamDocument>
    {
      Sort = sort
    }, cancellationToken);

    while (await cursor.MoveNextAsync(cancellationToken))
    {
      foreach (var document in cursor.Current)
      {
        yield return document;
      }
    }
  }

  public async IAsyncEnumerable<EventStreamDocument> ListDescendingAsync(
    ulong endVersion,
    ulong startVersion,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    var filter = Builders<EventStreamDocument>.Filter.And(
      Builders<EventStreamDocument>.Filter.Eq(d => d.StreamId, StreamId),
      Builders<EventStreamDocument>.Filter.Gte(d => d.Version, startVersion),
      Builders<EventStreamDocument>.Filter.Lte(d => d.Version, endVersion)
    );
    var sort = Builders<EventStreamDocument>.Sort.Descending(d => d.Version);

    using var cursor = await _documentsCollection.FindAsync(filter, new FindOptions<EventStreamDocument>
    {
      Sort = sort
    }, cancellationToken);

    while (await cursor.MoveNextAsync(cancellationToken))
    {
      foreach (var document in cursor.Current)
      {
        yield return document;
      }
    }
  }

  public async IAsyncEnumerable<EventStreamDocument> ListDescendingAsync(
    ulong endVersion,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    var filter = Builders<EventStreamDocument>.Filter.And(
      Builders<EventStreamDocument>.Filter.Eq(d => d.StreamId, StreamId),
      Builders<EventStreamDocument>.Filter.Lte(d => d.Version, endVersion)
    );
    var sort = Builders<EventStreamDocument>.Sort.Descending(d => d.Version);

    using var cursor = await _documentsCollection.FindAsync(filter, new FindOptions<EventStreamDocument>
    {
      Sort = sort
    }, cancellationToken);

    while (await cursor.MoveNextAsync(cancellationToken))
    {
      foreach (var document in cursor.Current)
      {
        yield return document;
      }
    }
  }

  public async Task UpdateStreamMetaData(EventStreamMetaData metaData, CancellationToken cancellationToken = default)
  {
    var filter = Builders<MongoEventStreamMetadata>.Filter.Eq(m => m.StreamId, StreamId);
    var update = Builders<MongoEventStreamMetadata>.Update.Set(m => m.MetaData, metaData);
    await _metadataCollection.UpdateOneAsync(filter, update, new UpdateOptions(), cancellationToken);
    MetaData = metaData;
  }
}
