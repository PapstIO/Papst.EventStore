using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Papst.EventStore.AzureCosmos.Database;

namespace Papst.EventStore.AzureCosmos;

internal sealed class CosmosEventStore(
  ILogger<CosmosEventStore> logger,
  ILoggerFactory loggerFactory,
  IOptions<CosmosEventStoreOptions> options,
  CosmosDatabaseProvider dbProvider,
  IEventTypeProvider eventTypeProvider,
  ICosmosIdStrategy idStrategy
)
  : IEventStore
{
  internal const string IndexDocumentName = "Index";

  public async Task<IEventStream> GetAsync(Guid streamId, CancellationToken cancellationToken = default)
  {
    Logging.GetEventStream(logger, streamId);
    ItemResponse<EventStreamIndexEntity>? stream = await dbProvider.Container
      .ReadItemAsync<EventStreamIndexEntity>(
        IndexDocumentName,
        new(streamId.ToString()),
        cancellationToken: cancellationToken).ConfigureAwait(false);

    return new CosmosEventStream(
      loggerFactory.CreateLogger<CosmosEventStream>(),
      options.Value,
      stream.Resource,
      dbProvider,
      eventTypeProvider,
      idStrategy);
  }

  public async Task<IEventStream> CreateAsync(
    Guid streamId,
    string targetTypeName,
    CancellationToken cancellationToken = default
  )
  {
    Logging.CreatingEventStream(logger, streamId, targetTypeName);

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

    ItemResponse<EventStreamIndexEntity>? response = await dbProvider.Container.CreateItemAsync(
      stream,
      new PartitionKey(streamId.ToString()),
      cancellationToken: cancellationToken).ConfigureAwait(false);

    return new CosmosEventStream(
      loggerFactory.CreateLogger<CosmosEventStream>(),
      options.Value,
      response.Resource,
      dbProvider,
      eventTypeProvider,
      idStrategy);
  }
}
