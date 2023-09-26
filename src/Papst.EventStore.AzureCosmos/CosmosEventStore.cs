using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Papst.EventStore.AzureCosmos.Database;

namespace Papst.EventStore.AzureCosmos;

internal sealed class CosmosEventStore : IEventStore
{
  internal const string IndexDocumentName = "Index";
  private readonly ILogger<CosmosEventStore> _logger;
  private readonly CosmosDatabaseProvider _dbProvider;
  private readonly ILoggerFactory _loggerFactory;
  private readonly IEventTypeProvider _eventTypeProvider;
  private readonly ICosmosIdStrategy _idStrategy;

  public CosmosEventStore(
    ILogger<CosmosEventStore> logger,
    ILoggerFactory loggerFactory,
    CosmosDatabaseProvider dbProvider,
    IEventTypeProvider eventTypeProvider,
    ICosmosIdStrategy idStrategy
  )
  {
    _logger = logger;
    _loggerFactory = loggerFactory;
    _dbProvider = dbProvider;
    _eventTypeProvider = eventTypeProvider;
    _idStrategy = idStrategy;
  }

  public async Task<IEventStream> GetAsync(Guid streamId, CancellationToken cancellationToken = default)
  {
    Logging.GetEventStream(_logger, streamId);
    ItemResponse<EventStreamIndexEntity>? stream = await _dbProvider.Container
      .ReadItemAsync<EventStreamIndexEntity>(
        IndexDocumentName,
        new(streamId.ToString()),
        cancellationToken: cancellationToken).ConfigureAwait(false);

    return new CosmosEventStream(
      _loggerFactory.CreateLogger<CosmosEventStream>(),
      stream.Resource,
      _dbProvider,
      _eventTypeProvider,
      _idStrategy);
  }

  public async Task<IEventStream> CreateAsync(
    Guid streamId,
    string targetTypeName,
    CancellationToken cancellationToken = default
  )
  {
    Logging.CreatingEventStream(_logger, streamId, targetTypeName);

    EventStreamIndexEntity stream = new()
    {
      StreamId = streamId,
      TargetType = targetTypeName,
      Updated = DateTimeOffset.Now,
      Version = 0,
      NextVersion = 0,
      Created = DateTimeOffset.Now,
      ETag = string.Empty,
      LatestSnapshotVersion = null,
    };

    ItemResponse<EventStreamIndexEntity>? response = await _dbProvider.Container.CreateItemAsync(
      stream,
      new PartitionKey(streamId.ToString()),
      cancellationToken: cancellationToken).ConfigureAwait(false);

    return new CosmosEventStream(
      _loggerFactory.CreateLogger<CosmosEventStream>(),
      response.Resource,
      _dbProvider,
      _eventTypeProvider,
      _idStrategy);
  }
}
