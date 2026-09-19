using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Papst.EventStore.Documents;
using Papst.EventStore.Pipeline;

namespace Papst.EventStore.MongoDB;

internal class MongoDBTransactionalBatch : IEventStoreTransactionAppender
{
  private readonly IMongoCollection<EventStreamDocument> _documentsCollection;
  private readonly IMongoCollection<MongoEventStreamMetadata> _metadataCollection;
  private readonly IEventTypeProvider _typeProvider;
  private readonly TimeProvider _timeProvider;
  private readonly Guid _streamId;
  private readonly string _targetType;
  private readonly IEventStorePipeline<EventAppendContext> _pipeline;
  private readonly ILogger _logger;
  private readonly List<EventStreamDocument> _pendingDocuments = new();

  public MongoDBTransactionalBatch(
    IMongoCollection<EventStreamDocument> documentsCollection,
    IMongoCollection<MongoEventStreamMetadata> metadataCollection,
    IEventTypeProvider typeProvider,
    TimeProvider timeProvider,
    Guid streamId,
    string targetType,
    IEventStorePipeline<EventAppendContext> pipeline,
    ILogger logger)
  {
    _documentsCollection = documentsCollection;
    _metadataCollection = metadataCollection;
    _typeProvider = typeProvider;
    _timeProvider = timeProvider;
    _streamId = streamId;
    _targetType = targetType;
    _pipeline = pipeline;
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

    // Run the pending documents through the append pipeline (which may sign them, chaining
    // onto the stored chain head), then persist the possibly-signed documents in one write.
    var context = new EventAppendContext
    {
      StreamId = _streamId,
      DocumentType = EventStreamDocumentType.Event,
      MetaData = null,
      PreviousSignature = metadata.LatestSignature,
      Entries = _pendingDocuments.Select(d => new EventAppendEntry(d)).ToList(),
    };

    await _pipeline.ExecuteAsync(context, () => PersistAsync(context, filter, baseVersion, cancellationToken), cancellationToken)
      .ConfigureAwait(false);
  }

  private async Task PersistAsync(EventAppendContext context, FilterDefinition<MongoEventStreamMetadata> filter, ulong baseVersion, CancellationToken cancellationToken)
  {
    List<EventStreamDocument> documents = context.Entries.Select(e => e.Document).ToList();
    ulong newVersion = baseVersion + (ulong)documents.Count - 1;
    EventSignature? lastSignature = documents[^1].Signature;

    UpdateDefinition<MongoEventStreamMetadata> BuildUpdate() => Builders<MongoEventStreamMetadata>.Update
      .Set(m => m.Version, newVersion)
      .Set(m => m.NextVersion, newVersion + 1)
      .Set(m => m.LatestSignature, lastSignature != null ? lastSignature.Value : context.PreviousSignature)
      .Set(m => m.SigningAlgorithm, lastSignature != null ? lastSignature.Algorithm : null)
      .Set(m => m.SigningCertificateThumbprint, lastSignature != null ? lastSignature.CertificateThumbprint : null);

    // Try to use a transaction if MongoDB supports it (replica set)
    // Otherwise fall back to non-transactional operation
    var client = _documentsCollection.Database.Client;
    try
    {
      using var session = await client.StartSessionAsync(cancellationToken: cancellationToken);
      session.StartTransaction();

      try
      {
        await _documentsCollection.InsertManyAsync(session, documents, new InsertManyOptions(), cancellationToken);
        await _metadataCollection.UpdateOneAsync(session, filter, BuildUpdate(), new UpdateOptions(), cancellationToken);

        await session.CommitTransactionAsync(cancellationToken);
        _logger.TransactionCompleted(_streamId, documents.Count);
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

      await _documentsCollection.InsertManyAsync(documents, new InsertManyOptions(), cancellationToken);
      await _metadataCollection.UpdateOneAsync(filter, BuildUpdate(), new UpdateOptions(), cancellationToken);
    }
  }
}
