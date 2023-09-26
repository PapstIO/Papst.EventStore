using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Papst.EventStore.AzureCosmos.Database;

internal class CosmosDatabaseProvider
{
  private readonly CosmosClient _client;
  private readonly string _databaseId;
  private readonly string _containerId;
  private readonly ILogger<CosmosDatabaseProvider> _logger;

  internal virtual Container Container => _client.GetContainer(_databaseId, _containerId);
  
  public CosmosDatabaseProvider(ILogger<CosmosDatabaseProvider> logger, CosmosClient client, string databaseId, string containerId)
  {
    _logger = logger;
    _client = client;
    _databaseId = databaseId;
    _containerId = containerId;
  }
  
  
}
