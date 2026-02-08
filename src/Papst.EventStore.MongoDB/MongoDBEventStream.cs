using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Papst.EventStore.Documents;

namespace Papst.EventStore.MongoDB;

internal class MongoDBEventStream : IEventStream
{
  private readonly IMongoCollection<EventStreamDocument> _documentsCollection;
  private readonly IMongoCollection<MongoEventStreamMetadata> _metadataCollection;
  private readonly TimeProvider _timeProvider;
  private readonly string _targetType;
  private readonly IEventTypeProvider _typeProvider;

  public MongoDBEventStream(
    Guid streamId,
    ulong version,
    DateTimeOffset created,
    EventStreamMetaData metaData,
    string targetType,
    TimeProvider timeProvider,
    IEventTypeProvider typeProvider,
    IMongoCollection<EventStreamDocument> documentsCollection,
    IMongoCollection<MongoEventStreamMetadata> metadataCollection)
  {
    StreamId = streamId;
    Version = version;
    Created = created;
    MetaData = metaData;
    _targetType = targetType;
    _timeProvider = timeProvider;
    _typeProvider = typeProvider;
    _documentsCollection = documentsCollection;
    _metadataCollection = metadataCollection;
  }

  public Guid StreamId { get; }
  public ulong Version { get; private set; }
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

  public async Task AppendAsync<TEvent>(
    Guid id,
    TEvent evt,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default) where TEvent : notnull
  {
    string name = _typeProvider.ResolveType(typeof(TEvent));
    var newVersion = Version + 1;

    var document = new EventStreamDocument
    {
      Id = id,
      StreamId = StreamId,
      Version = newVersion,
      Time = _timeProvider.GetLocalNow(),
      DataType = name,
      Data = JObject.FromObject(evt),
      DocumentType = EventStreamDocumentType.Event,
      MetaData = metaData ?? new EventStreamMetaData(),
      TargetType = _targetType,
      Name = name
    };

    await _documentsCollection.InsertOneAsync(document, new InsertOneOptions(), cancellationToken);

    // Update version in metadata
    var filter = Builders<MongoEventStreamMetadata>.Filter.Eq(m => m.StreamId, StreamId);
    var update = Builders<MongoEventStreamMetadata>.Update.Set(m => m.Version, newVersion);
    await _metadataCollection.UpdateOneAsync(filter, update, new UpdateOptions(), cancellationToken);

    Version = newVersion;
  }

  public async Task AppendSnapshotAsync<TEntity>(
    Guid id,
    TEntity entity,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default) where TEntity : notnull
  {
    var newVersion = Version + 1;

    var document = new EventStreamDocument
    {
      Id = id,
      StreamId = StreamId,
      Version = newVersion,
      Time = _timeProvider.GetLocalNow(),
      DataType = _targetType,
      Data = JObject.FromObject(entity),
      DocumentType = EventStreamDocumentType.Snapshot,
      MetaData = metaData ?? new EventStreamMetaData(),
      TargetType = _targetType,
      Name = _targetType
    };

    await _documentsCollection.InsertOneAsync(document, new InsertOneOptions(), cancellationToken);

    // Update version and snapshot version in metadata
    var filter = Builders<MongoEventStreamMetadata>.Filter.Eq(m => m.StreamId, StreamId);
    var update = Builders<MongoEventStreamMetadata>.Update
      .Set(m => m.Version, newVersion)
      .Set(m => m.LatestSnapshotVersion, newVersion);
    await _metadataCollection.UpdateOneAsync(filter, update, new UpdateOptions(), cancellationToken);

    Version = newVersion;
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
        _targetType
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
