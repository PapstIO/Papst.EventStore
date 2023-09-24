using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using Papst.EventStore.Documents;
using Papst.EventStore.EntityFrameworkCore.Database;

namespace Papst.EventStore.EntityFrameworkCore;
internal class EntityFrameworkEventStream : IEventStream
{
  private ILogger<EntityFrameworkEventStream> _logger;
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


  public Task<IEventStoreBatchAppender> AppendBatchAsync()
  {
    throw new NotImplementedException();
  }

  public Task<EventStreamDocument?> GetLatestSnapshot(CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public IAsyncEnumerable<EventStreamDocument> ListAsync(ulong startVersion, CancellationToken cancellationToken = default) 
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
      UserId = doc.MetaDataUserId,
      UserName = doc.MetaDataUserName,
      TenantId = doc.MetaDataTenantId,
      Comment = doc.MetaDataComment,
      Additional = JsonSerializer.Deserialize<Dictionary<string, string>>(doc.MetaDataAdditional, (JsonSerializerOptions?)null)
    },
  };

  private EventStreamDocumentEntity CreateEventEntity<TEvent>(Guid id, TEvent evt, EventStreamMetaData? metaData, string eventName)
    where TEvent : notnull
  {
    EventStreamDocumentEntity document = new()
    {
      Id = id,
      StreamId = StreamId,
      Type = EventStreamDocumentEntityType.Event,
      Version = _stream.NextVersion,
      Time = DateTimeOffset.Now,
      Name = eventName,
      DataType = eventName,
      TargetType = _stream.TargetType,
      Data = JsonSerializer.Serialize(evt),
      MetaDataUserId = metaData?.UserId,
      MetaDataUserName = metaData?.UserName,
      MetaDataTenantId = metaData?.TenantId,
      MetaDataComment = metaData?.Comment,
      MetaDataAdditional = metaData?.Additional != null ? JsonSerializer.Serialize(metaData.Additional) : "{}",
    };
    
    return document;
  }
}
