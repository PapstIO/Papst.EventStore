using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Papst.EventStore.AzureCosmos.Database;
using Papst.EventStore.Documents;

namespace Papst.EventStore.AzureCosmos;

internal sealed class CosmosEventStream : IEventStream
{
  private EventStreamIndexEntity _stream;
  private readonly ILogger<CosmosEventStream> _logger;
  private readonly CosmosDatabaseProvider _dbProvider;
  private readonly IEventTypeProvider _eventTypeProvider;
  private readonly ICosmosIdStrategy _idStrategy;

  public Guid StreamId => _stream.StreamId;
  public ulong Version => _stream.Version;
  public DateTimeOffset Created => _stream.Created;

  public CosmosEventStream(
    ILogger<CosmosEventStream> logger,
    EventStreamIndexEntity stream,
    CosmosDatabaseProvider dbProvider,
    IEventTypeProvider eventTypeProvider,
    ICosmosIdStrategy idStrategy
  )
  {
    _logger = logger;
    _stream = stream;
    _dbProvider = dbProvider;
    _eventTypeProvider = eventTypeProvider;
    _idStrategy = idStrategy;
  }

  public Task<EventStreamDocument?> GetLatestSnapshot(CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public async Task AppendAsync<TEvent>(
    Guid id,
    TEvent evt,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default
  ) where TEvent : notnull
  {
    string eventName = _eventTypeProvider.ResolveType(typeof(TEvent));
    EventStreamDocumentEntity document = await CreateEventEntity(id, evt, metaData, eventName).ConfigureAwait(false);

    Logging.AppendingEvent(_logger, document.DataType, document.StreamId, document.Version);
    await _dbProvider.Container.CreateItemAsync(
        document,
        new(StreamId.ToString()),
        cancellationToken: cancellationToken)
      .ConfigureAwait(false);

    ItemResponse<EventStreamIndexEntity>? indexPatch = await _dbProvider.Container
      .PatchItemAsync<EventStreamIndexEntity>(
        _stream.Id,
        new(StreamId.ToString()),
        new List<PatchOperation>()
        {
          PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.NextVersion), _stream.NextVersion + 1),
          PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.Version), _stream.NextVersion),
          PatchOperation.Replace('/' + nameof(EventStreamIndexEntity.Updated), DateTimeOffset.Now),
        },
        new PatchItemRequestOptions() { IfMatchEtag = _stream.ETag },
        cancellationToken).ConfigureAwait(false);

    _stream = indexPatch.Resource;
  }

  private async ValueTask<EventStreamDocumentEntity> CreateEventEntity<TEvent>(
    Guid id,
    TEvent evt,
    EventStreamMetaData? metaData,
    string eventName
  ) where TEvent : notnull => new()
  {
    Id = await _idStrategy.GenerateId(id, StreamId, _stream.NextVersion, EventStreamDocumentType.Event),
    DocumentId = id,
    StreamId = StreamId,
    Version = _stream.NextVersion,
    Data = JObject.FromObject(evt),
    DataType = eventName,
    Name = eventName,
    Time = DateTimeOffset.Now,
    DocumentType = EventStreamDocumentType.Event,
    MetaData = metaData ?? new(),
    TargetType = _stream.TargetType,
  };

  public Task<IEventStoreBatchAppender> AppendBatchAsync()
  {
    throw new NotImplementedException();
  }

  public IAsyncEnumerable<EventStreamDocument> ListAsync(
    ulong startVersion = 0u,
    CancellationToken cancellationToken = default
  )
  {
    throw new NotImplementedException();
  }

  public IAsyncEnumerable<EventStreamDocument> ListAsync(
    ulong startVersion,
    ulong endVersion,
    CancellationToken cancellationToken = default
  )
  {
    throw new NotImplementedException();
  }
}
