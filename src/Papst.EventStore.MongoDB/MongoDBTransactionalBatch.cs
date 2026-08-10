using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Papst.EventStore.Documents;

namespace Papst.EventStore.MongoDB;

internal class MongoDBTransactionalBatch : IEventStoreTransactionAppender
{
  private readonly IMongoCollection<EventStreamDocument> _documentsCollection;
  private readonly IMongoCollection<MongoEventStreamMetadata> _metadataCollection;
  private readonly IEventTypeProvider _typeProvider;
  private readonly TimeProvider _timeProvider;
  private readonly Guid _streamId;
  private readonly string _targetType;
  private readonly ILogger _logger;
  private readonly List<EventStreamDocument> _pendingDocuments = new();

  public MongoDBTransactionalBatch(
    IMongoCollection<EventStreamDocument> documentsCollection,
    IMongoCollection<MongoEventStreamMetadata> metadataCollection,
    IEventTypeProvider typeProvider,
    TimeProvider timeProvider,
    Guid streamId,
    string targetType,
    ILogger logger)
  {
    _documentsCollection = documentsCollection;
    _metadataCollection = metadataCollection;
    _typeProvider = typeProvider;
    _timeProvider = timeProvider;
    _streamId = streamId;
    _targetType = targetType;
    _logger = logger;
  }

  public IEventStoreTransactionAppender Add<TEvent>(
    Guid id,
    TEvent evt,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default) where TEvent : notnull
  {
    string name = _typeProvider.ResolveType(typeof(TEvent));

    var document = new EventStreamDocument
    {
      Id = id,
      StreamId = _streamId,
      Version = 0, // Will be set during commit
      Time = _timeProvider.GetLocalNow(),
      DataType = name,
      Data = JObject.FromObject(evt),
      DocumentType = EventStreamDocumentType.Event,
      MetaData = metaData ?? new EventStreamMetaData(),
      TargetType = _targetType,
      Name = name
    };

    _pendingDocuments.Add(document);
    return this;
  }

  public async Task CommitAsync(CancellationToken cancellationToken = default)
  {
    if (_pendingDocuments.Count == 0)
    {
      return;
    }

    // Get current version
    var filter = Builders<MongoEventStreamMetadata>.Filter.Eq(m => m.StreamId, _streamId);
    var metadata = await _metadataCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);

    if (metadata == null)
    {
      throw new InvalidOperationException($"Stream {_streamId} not found");
    }

    // NextVersion is the version to assign to the first pending document (0 on a freshly
    // created stream), so events are numbered 0-based like the other stores.
    ulong baseVersion = metadata.NextVersion;

    // Assign versions to pending documents
    for (int i = 0; i < _pendingDocuments.Count; i++)
    {
      _pendingDocuments[i] = _pendingDocuments[i] with { Version = baseVersion + (ulong)i };
    }

    // Try to use a transaction if MongoDB supports it (replica set)
    // Otherwise fall back to non-transactional operation
    var client = _documentsCollection.Database.Client;
    try
    {
      using var session = await client.StartSessionAsync(cancellationToken: cancellationToken);
      session.StartTransaction();

      try
      {
        // Insert all documents
        await _documentsCollection.InsertManyAsync(session, _pendingDocuments, new InsertManyOptions(), cancellationToken);

        // Update version in metadata
        var newVersion = baseVersion + (ulong)_pendingDocuments.Count - 1;
        var update = Builders<MongoEventStreamMetadata>.Update
          .Set(m => m.Version, newVersion)
          .Set(m => m.NextVersion, newVersion + 1);
        await _metadataCollection.UpdateOneAsync(session, filter, update, new UpdateOptions(), cancellationToken);

        await session.CommitTransactionAsync(cancellationToken);
        _logger.TransactionCompleted(_streamId, _pendingDocuments.Count);
      }
      catch (Exception ex)
      {
        if (session.IsInTransaction)
        {
          await session.AbortTransactionAsync(cancellationToken);
        }
        _logger.TransactionException(ex, _streamId);
        throw;
      }
    }
    catch (System.NotSupportedException)
    {
      // MongoDB standalone doesn't support transactions, fall back to non-transactional
      _logger.TransactionNotSupported(_streamId);
      
      await _documentsCollection.InsertManyAsync(_pendingDocuments, new InsertManyOptions(), cancellationToken);

      var newVersion = baseVersion + (ulong)_pendingDocuments.Count - 1;
      var update = Builders<MongoEventStreamMetadata>.Update
        .Set(m => m.Version, newVersion)
        .Set(m => m.NextVersion, newVersion + 1);
      await _metadataCollection.UpdateOneAsync(filter, update, new UpdateOptions(), cancellationToken);
    }
  }
}
