using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Papst.EventStore.Documents;
using Papst.EventStore.EntityFrameworkCore.Database;
using Papst.EventStore.Pipeline;

namespace Papst.EventStore.EntityFrameworkCore;
internal sealed class EntityFrameworkEventStream : IEventStream, ILowLevelEventStream
{
  private readonly ILogger<EntityFrameworkEventStream> _logger;
  private readonly EventStoreDbContext _dbContext;
  private readonly EventStreamEntity _stream;
  private readonly IEventTypeProvider _eventTypeProvider;
  private readonly IEventStorePipeline<EventAppendContext> _pipeline;

  public EntityFrameworkEventStream(
    ILogger<EntityFrameworkEventStream> logger,
    EventStoreDbContext dbContext,
    EventStreamEntity stream,
    IEventTypeProvider eventTypeProvider,
    IEventStorePipeline<EventAppendContext> pipeline
  )
  {
    _logger = logger;
    _dbContext = dbContext;
    _stream = stream;
    _eventTypeProvider = eventTypeProvider;
    _pipeline = pipeline;
    MetaData = new EventStreamMetaData
    {
      UserId = _stream.MetaDataUserId,
      UserName = _stream.MetaDataUserName,
      TenantId = _stream.MetaDataTenantId,
      Comment = _stream.MetaDataComment,
      Additional = JsonSerializer.Deserialize<Dictionary<string, string>>(_stream.MetaDataAdditionJson ?? "{}")
    };
  }

  public Guid StreamId => _stream.StreamId;

  public ulong Version => _stream.Version;

  public DateTimeOffset Created => _stream.Created;
  
  public ulong? LatestSnapshotVersion => _stream.LatestSnapshotVersion;
  
  /// <inheritdoc />
  public EventStreamMetaData MetaData { get; }

  public Task AppendAsync<TEvent>(
    Guid id,
    TEvent evt,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default
  ) where TEvent : notnull
  {
    string eventName = _eventTypeProvider.ResolveType(typeof(TEvent));
    EventStreamDocument document = BuildDocument(id, JObject.FromObject(evt), metaData, eventName, EventStreamDocumentType.Event);
    return AppendThroughPipelineAsync([document], EventStreamDocumentType.Event, metaData, cancellationToken);
  }

  public Task AppendAsync(
    Guid id,
    string eventType,
    JObject evt,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default
  )
  {
    EventStreamDocument document = BuildDocument(id, evt, metaData, eventType, EventStreamDocumentType.Event);
    return AppendThroughPipelineAsync([document], EventStreamDocumentType.Event, metaData, cancellationToken);
  }

  public Task AppendSnapshotAsync<TEntity>(
    Guid id,
    TEntity entity,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default
  ) where TEntity : notnull
  {
    string eventName = typeof(TEntity).Name;
    EventStreamDocument document = BuildDocument(id, JObject.FromObject(entity), metaData, eventName, EventStreamDocumentType.Snapshot);
    return AppendThroughPipelineAsync([document], EventStreamDocumentType.Snapshot, metaData, cancellationToken);
  }

  /// <summary>
  /// Runs the documents through the append pipeline (which may sign them) and then
  /// maps and persists them, mirroring the chain head onto the stream index.
  /// </summary>
  private async Task AppendThroughPipelineAsync(
    IReadOnlyList<EventStreamDocument> documents,
    EventStreamDocumentType documentType,
    EventStreamMetaData? metaData,
    CancellationToken cancellationToken)
  {
    if (documents.Count == 0)
    {
      return;
    }

    var context = new EventAppendContext
    {
      StreamId = StreamId,
      DocumentType = documentType,
      MetaData = metaData,
      PreviousSignature = _stream.LatestSignature,
      Entries = documents.Select(d => new EventAppendEntry(d)).ToList(),
    };

    await _pipeline.ExecuteAsync(context, async () =>
    {
      foreach (EventAppendEntry entry in context.Entries)
      {
        EventStreamDocumentEntity entity = MapToEntity(entry.Document, documentType);
        Logging.AppendingEvent(_logger, entity.DataType, entity.StreamId, entity.Version);
        await _dbContext.Documents.AddAsync(entity, cancellationToken).ConfigureAwait(false);
      }

      EventStreamDocument last = context.Entries[^1].Document;
      _stream.Version = last.Version;
      _stream.NextVersion = last.Version + 1;
      _stream.Updated = DateTimeOffset.Now;
      if (documentType == EventStreamDocumentType.Snapshot)
      {
        _stream.LatestSnapshotVersion = last.Version;
      }

      if (last.Signature is not null)
      {
        _stream.LatestSignature = last.Signature.Value;
        _stream.SigningAlgorithm = last.Signature.Algorithm;
        _stream.SigningCertificateThumbprint = last.Signature.CertificateThumbprint;
      }

      _dbContext.Streams.Attach(_stream);
      await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }, cancellationToken).ConfigureAwait(false);
  }

  public Task<IEventStoreTransactionAppender> CreateTransactionalBatchAsync()
    => Task.FromResult<IEventStoreTransactionAppender>(new EntityFrameworkCoreTransactionalBatchAppender(this));
  

  public async Task<EventStreamDocument?> GetLatestSnapshot(CancellationToken cancellationToken = default)
  {
    if (!_stream.LatestSnapshotVersion.HasValue)
    {
      return null;
    }

    EventStreamDocumentEntity? document = await _dbContext.Documents
      .FirstOrDefaultAsync(
        doc => doc.StreamId == StreamId && doc.Version == _stream.LatestSnapshotVersion.Value,
        cancellationToken
      ).ConfigureAwait(false);

    if (document is null)
    {
      return null;
    }

    return Map()
      .Compile()
      .Invoke(document);
  }

  public IAsyncEnumerable<EventStreamDocument> ListAsync(ulong startVersion = 0u, CancellationToken cancellationToken = default) 
    => ListAsync(startVersion, _stream.Version, cancellationToken);

  public IAsyncEnumerable<EventStreamDocument> ListAsync(ulong startVersion, ulong endVersion, CancellationToken cancellationToken = default) =>
    _dbContext.Documents
      .Where(doc => 
        doc.StreamId == StreamId 
        && doc.Version >= startVersion 
        && doc.Version <= endVersion)
      .OrderBy(doc => doc.Version)
      .Select(Map())
      .AsNoTracking()
      .AsAsyncEnumerable();

  public IAsyncEnumerable<EventStreamDocument> ListDescendingAsync(ulong endVersion, ulong startVersion,
    CancellationToken cancellationToken = default)
    => _dbContext.Documents
      .Where(doc => 
        doc.StreamId == StreamId 
        && doc.Version <= endVersion
        && doc.Version >= startVersion)
      .OrderByDescending(doc => doc.Version)
      .Select(Map())
      .AsNoTracking()
      .AsAsyncEnumerable();

  public IAsyncEnumerable<EventStreamDocument> ListDescendingAsync(ulong endVersion, CancellationToken cancellationToken = default)
  => ListDescendingAsync(endVersion, 0u, cancellationToken);

  public async Task UpdateStreamMetaData(EventStreamMetaData metaData, CancellationToken cancellationToken = default)
  {
    _dbContext.Streams.Attach(_stream);
    _stream.MetaDataUserId = metaData.UserId;
    _stream.MetaDataTenantId = metaData.TenantId;
    _stream.MetaDataUserName = metaData.UserName;
    _stream.MetaDataComment = metaData.Comment;
    _stream.MetaDataAdditionJson = JsonSerializer.Serialize(metaData);
    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  private static Expression<Func<EventStreamDocumentEntity, EventStreamDocument>> Map() => doc => new EventStreamDocument()
  {
    Id = doc.Id,
    StreamId = doc.StreamId,
    DocumentType = doc.Type == EventStreamDocumentEntityType.Event ? EventStreamDocumentType.Event : EventStreamDocumentType.Snapshot,
    Version = doc.Version,
    Time = doc.Time,
    Name = doc.Name,
    Data = JObject.Parse(doc.Data),
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

  // Builds the storage-agnostic core document that the append pipeline operates on.
  private EventStreamDocument BuildDocument(Guid id, JObject data, EventStreamMetaData? metaData, string eventName, EventStreamDocumentType documentType)
    => new()
    {
      Id = id,
      StreamId = StreamId,
      DocumentType = documentType,
      Version = _stream.NextVersion,
      Time = DateTimeOffset.Now,
      Name = eventName,
      DataType = eventName,
      TargetType = _stream.TargetType,
      Data = data,
      MetaData = metaData ?? new(),
    };

  // Maps a (possibly signed) core document onto the EF entity.
  private static EventStreamDocumentEntity MapToEntity(EventStreamDocument doc, EventStreamDocumentType documentType)
    => new()
    {
      Id = doc.Id,
      StreamId = doc.StreamId,
      Type = documentType == EventStreamDocumentType.Snapshot
        ? EventStreamDocumentEntityType.Snapshot
        : EventStreamDocumentEntityType.Event,
      Version = doc.Version,
      Time = doc.Time,
      Name = doc.Name,
      DataType = doc.DataType,
      TargetType = doc.TargetType,
      Data = doc.Data.ToString(Newtonsoft.Json.Formatting.None),
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

  private class EntityFrameworkCoreTransactionalBatchAppender(EntityFrameworkEventStream stream)
    : IEventStoreTransactionAppender
  {
    private readonly List<(Guid Id, JObject Data, EventStreamMetaData? MetaData, string Name)> _items = [];

    public IEventStoreTransactionAppender Add<TEvent>(
      Guid id,
      TEvent evt,
      EventStreamMetaData? metaData = null,
      CancellationToken cancellationToken = default
    ) where TEvent: notnull
    {
      _items.Add((id, JObject.FromObject(evt), metaData, stream._eventTypeProvider.ResolveType(evt.GetType())));
      return this;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
      if (_items.Count == 0)
      {
        return Task.CompletedTask;
      }

      ulong version = stream._stream.NextVersion;
      List<EventStreamDocument> documents = _items
        .Select(item => stream.BuildDocument(item.Id, item.Data, item.MetaData, item.Name, EventStreamDocumentType.Event)
          with { Version = version++ })
        .ToList();

      return stream.AppendThroughPipelineAsync(documents, EventStreamDocumentType.Event, null, cancellationToken);
    }
  }
}
