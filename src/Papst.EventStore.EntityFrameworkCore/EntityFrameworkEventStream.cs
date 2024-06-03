using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Papst.EventStore.Documents;
using Papst.EventStore.EntityFrameworkCore.Database;

namespace Papst.EventStore.EntityFrameworkCore;
internal sealed class EntityFrameworkEventStream : IEventStream
{
  private readonly ILogger<EntityFrameworkEventStream> _logger;
  private readonly EventStoreDbContext _dbContext;
  private readonly EventStreamEntity _stream;
  private readonly IEventTypeProvider _eventTypeProvider;

  public EntityFrameworkEventStream(
    ILogger<EntityFrameworkEventStream> logger,
    EventStoreDbContext dbContext,
    EventStreamEntity stream,
    IEventTypeProvider eventTypeProvider
  )
  {
    _logger = logger;
    _dbContext = dbContext;
    _stream = stream;
    _eventTypeProvider = eventTypeProvider;
  }

  public Guid StreamId => _stream.StreamId;

  public ulong Version => _stream.Version;

  public DateTimeOffset Created => _stream.Created;
  
  public ulong? LatestSnapshotVersion => _stream.LatestSnapshotVersion;

  public async Task AppendAsync<TEvent>(
    Guid id,
    TEvent evt,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default
  ) where TEvent : notnull
  {
    string eventName = _eventTypeProvider.ResolveType(typeof(TEvent));
    EventStreamDocumentEntity document = CreateEventEntity(id, evt, metaData, eventName);
    _stream.Version = _stream.NextVersion;
    _stream.NextVersion++;
    _stream.Updated = DateTimeOffset.Now;
    
    Logging.AppendingEvent(_logger, document.DataType, document.StreamId, document.Version);
    _dbContext.Streams.Attach(_stream);
    await _dbContext.Documents.AddAsync(document, cancellationToken).ConfigureAwait(false);
    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }
  
  public async Task AppendSnapshotAsync<TEntity>(
    Guid id,
    TEntity entity,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default
  ) where TEntity : notnull
  {
    string eventName = typeof(TEntity).Name;
    EventStreamDocumentEntity document = CreateEventEntity(id, entity, metaData, eventName);
    _stream.Version = _stream.NextVersion;
    _stream.NextVersion++;
    _stream.Updated = DateTimeOffset.Now;
    _stream.LatestSnapshotVersion = _stream.Version;
    
    Logging.AppendingEvent(_logger, document.DataType, document.StreamId, document.Version);
    _dbContext.Streams.Attach(_stream);
    await _dbContext.Documents.AddAsync(document, cancellationToken).ConfigureAwait(false);
    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }


  public Task<IEventStoreTransactionAppender> CreateTransactionalBatchAsync()
    => Task.FromResult<IEventStoreTransactionAppender>(new EntityFrameworkCoreTransactionalBatchAppender(this, _dbContext));
  

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
      .Where(doc => doc.StreamId == StreamId && doc.Version >= startVersion && doc.Version <= endVersion)
      .OrderBy(doc => doc.Version)
      .Select(Map())
      .AsNoTracking()
      .AsAsyncEnumerable();

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
    MetaData = new()
    {
      UserId = doc.MetaData.UserId,
      UserName = doc.MetaData.UserName,
      TenantId = doc.MetaData.TenantId,
      Comment = doc.MetaData.Comment,
      Additional = doc.MetaData.Additional,
    },
  };

  private EventStreamDocumentEntity CreateEventEntity<TEvent>(Guid id, TEvent evt, EventStreamMetaData? metaData, string eventName, EventStreamDocumentEntityType documentType = EventStreamDocumentEntityType.Event)
    where TEvent : notnull
  {
    EventStreamDocumentEntity document = new()
    {
      Id = id,
      StreamId = StreamId,
      Type = documentType,
      Version = _stream.NextVersion,
      Time = DateTimeOffset.Now,
      Name = eventName,
      DataType = eventName,
      TargetType = _stream.TargetType,
      Data = JsonSerializer.Serialize(evt),
      MetaData = new()
      {
        UserId = metaData?.UserId,
        UserName = metaData?.UserName,
        TenantId = metaData?.TenantId,
        Comment = metaData?.Comment,
        Additional = metaData?.Additional
      }
    };
    
    return document;
  }

  private class EntityFrameworkCoreTransactionalBatchAppender(EntityFrameworkEventStream stream, EventStoreDbContext context) 
    : IEventStoreTransactionAppender
  {
    private readonly List<(Guid Id, object Evt, EventStreamMetaData? MetaData, EventStreamDocumentEntity Entity)> _items = [];
    public IEventStoreTransactionAppender Add<TEvent>(
      Guid id,
      TEvent evt,
      EventStreamMetaData? metaData = null,
      CancellationToken cancellationToken = default
    ) where TEvent: notnull
    {
      EventStreamDocumentEntity entity = stream.CreateEventEntity(
        id,
        evt,
        metaData,
        stream._eventTypeProvider.ResolveType(evt.GetType())
      );
      _items.Add((id, evt, metaData, entity));

      return this;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
      if (_items.Count == 0)
      {
        return;
      }
      
      foreach ((Guid Id, object Evt, EventStreamMetaData? MetaData, EventStreamDocumentEntity Entity) item in _items)
      {
        Logging.AppendingEvent(stream._logger, item.Entity.DataType, stream.StreamId, item.Entity.Version);
        await context.Documents.AddAsync(item.Entity, cancellationToken).ConfigureAwait(false);
      }
      
      stream._stream.Version = _items.Max(i => i.Entity.Version);
      stream._stream.NextVersion = stream._stream.Version + 1;
      stream._stream.Updated = DateTimeOffset.Now;
      context.Streams.Attach(stream._stream);
      
      await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
  }
}
