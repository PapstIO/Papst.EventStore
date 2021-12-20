using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Papst.EventStore.CosmosDb.CosmosClients;

/// <summary>
/// Cosmos Client Implementation based on Shared Secret Authentication
/// </summary>
internal class SharedSecretCosmosClient : EventStoreCosmosClientBase
{
  public SharedSecretCosmosClient(
      ILogger<SharedSecretCosmosClient> logger,
      IOptions<CosmosEventStoreOptions> options
      )
      : base(
          logger,
          new CosmosClient(options.Value.Endpoint, options.Value.AccountSecret),
          options.Value)
  {
    if (string.IsNullOrEmpty(options.Value.AccountSecret))
    {
      throw new ArgumentException("AccountSecret must not be null or empty", nameof(options));
    }
    logger.LogInformation("Created SharedSecret based Cosmos Client");
  }
}

