using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Papst.EventStore.Documents;
using Papst.EventStore.Exceptions;

namespace Papst.EventStore.MongoDB;

public class MongoDBEventStore : IEventStore
{
  private readonly IMongoDatabase _database;
  private readonly IMongoCollection<EventStreamDocument> _documentsCollection;
  private readonly IMongoCollection<MongoEventStreamMetadata> _metadataCollection;
  private readonly TimeProvider _timeProvider;
  private readonly IEventTypeProvider _eventTypeProvider;
  private readonly MongoDBEventStoreOptions _options;
  private readonly System.Threading.SemaphoreSlim _indexCreationLock = new(1, 1);
  private bool _indexesCreated = false;
  private static readonly object _conventionLock = new();
  private static bool _conventionsRegistered = false;

  public MongoDBEventStore(
    IOptions<MongoDBEventStoreOptions> options,
    TimeProvider timeProvider,
    IEventTypeProvider eventTypeProvider)
  {
    _options = options.Value;
    _timeProvider = timeProvider;
    _eventTypeProvider = eventTypeProvider;

    // Register BSON conventions for GUID serialization as strings
    RegisterConventions();

    var client = new MongoClient(_options.ConnectionString);
    _database = client.GetDatabase(_options.DatabaseName);
    _documentsCollection = _database.GetCollection<EventStreamDocument>(_options.CollectionName);
    _metadataCollection = _database.GetCollection<MongoEventStreamMetadata>(_options.StreamMetadataCollectionName);
  }

  private static void RegisterConventions()
  {
    lock (_conventionLock)
    {
      if (_conventionsRegistered) return;

      // Register convention to serialize GUIDs as strings
      var guidSerializer = new GuidSerializer(BsonType.String);
      BsonSerializer.RegisterSerializer(guidSerializer);

      // Register custom serializer for JObject
      BsonSerializer.RegisterSerializer(typeof(Newtonsoft.Json.Linq.JObject), new JObjectSerializer());

      _conventionsRegistered = true;
    }
  }

  private async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
  {
    if (_indexesCreated) return;

    await _indexCreationLock.WaitAsync(cancellationToken);
    try
    {
      if (_indexesCreated) return;

      // Index on StreamId and Version for efficient querying
      var documentsIndexKeysDefinition = Builders<EventStreamDocument>.IndexKeys
        .Ascending(d => d.StreamId)
        .Ascending(d => d.Version);
      var documentsIndexModel = new CreateIndexModel<EventStreamDocument>(documentsIndexKeysDefinition);
      await _documentsCollection.Indexes.CreateOneAsync(documentsIndexModel, cancellationToken: cancellationToken);

      // Unique index on StreamId for metadata
      var metadataIndexKeysDefinition = Builders<MongoEventStreamMetadata>.IndexKeys
        .Ascending(m => m.StreamId);
      var metadataIndexModel = new CreateIndexModel<MongoEventStreamMetadata>(
        metadataIndexKeysDefinition,
        new CreateIndexOptions { Unique = true }
      );
      await _metadataCollection.Indexes.CreateOneAsync(metadataIndexModel, cancellationToken: cancellationToken);

      _indexesCreated = true;
    }
    finally
    {
      _indexCreationLock.Release();
    }
  }

  public async Task<IEventStream> GetAsync(Guid streamId, CancellationToken cancellationToken = default)
  {
    var filter = Builders<MongoEventStreamMetadata>.Filter.Eq(m => m.StreamId, streamId);
    var metadata = await _metadataCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);

    if (metadata == null)
    {
      throw new EventStreamNotFoundException(streamId, "Stream not found in MongoDB");
    }

    return new MongoDBEventStream(
      metadata.StreamId,
      metadata.Version,
      metadata.Created,
      metadata.MetaData,
      metadata.TargetTypeName,
      _timeProvider,
      _eventTypeProvider,
      _documentsCollection,
      _metadataCollection
    );
  }

  public Task<IEventStream> CreateAsync(
    Guid streamId,
    string targetTypeName,
    CancellationToken cancellationToken = default) =>
    CreateAsync(streamId, targetTypeName, null, null, null, null, null, cancellationToken);

  public async Task<IEventStream> CreateAsync(
    Guid streamId,
    string targetTypeName,
    string? tenantId,
    string? userId,
    string? username,
    string? comment,
    System.Collections.Generic.Dictionary<string, string>? additionalMetaData,
    CancellationToken cancellationToken = default)
  {
    var metadata = new MongoEventStreamMetadata
    {
      StreamId = streamId,
      Version = 0,
      Created = _timeProvider.GetLocalNow(),
      TargetTypeName = targetTypeName,
      MetaData = new EventStreamMetaData
      {
        TenantId = tenantId,
        UserId = userId,
        UserName = username,
        Comment = comment,
        Additional = additionalMetaData
      },
      LatestSnapshotVersion = null
    };

    try
    {
      await _metadataCollection.InsertOneAsync(metadata, new InsertOneOptions(), cancellationToken);
    }
    catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
    {
      throw new EventStreamAlreadyExistsException(streamId, "Stream already exists in MongoDB");
    }

    return new MongoDBEventStream(
      metadata.StreamId,
      metadata.Version,
      metadata.Created,
      metadata.MetaData,
      metadata.TargetTypeName,
      _timeProvider,
      _eventTypeProvider,
      _documentsCollection,
      _metadataCollection
    );
  }
}
