using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Papst.EventStore.CosmosDb.CosmosClients;

internal class EventStoreCosmosClient : IEventStoreCosmosClient
{
  private readonly ILogger _logger;
  private readonly CosmosClient _client;

  /// <<inheritdoc/>
  public bool InitializeOnStartup => _options.InitializeOnStartup;

  /// <<inheritdoc/>
  public bool AllowTimeOverride => _options.AllowTimeOverride;

  /// <<inheritdoc/>
  public string Collection => _options.Collection;

  /// <<inheritdoc/>
  public bool IsAlreadyInitialized { get; private set; }

  private readonly CosmosEventStoreOptions _options;

  public EventStoreCosmosClient(
      ILogger<EventStoreCosmosClient> logger,
      IOptions<CosmosEventStoreOptions> options)
  {
    _logger = logger;
    if (options.Value.Credential is not null)
    {
      _client = new CosmosClient(options.Value.Endpoint, options.Value.Credential);
    }
    else if (!string.IsNullOrWhiteSpace(options.Value.AccountSecret))
    {
      _logger.LogWarning("Connecting to CosmosDB using Shared Secret - Migrate to Managed Identity!");
      _client = new CosmosClient(options.Value.Endpoint, options.Value.AccountSecret);
    }
    else
    {
      throw new ArgumentNullException(nameof(options));
    }
    _options = options.Value;
    IsAlreadyInitialized = false;
  }

  /// <<inheritdoc/>
  public Container GetContainer() => _client.GetContainer(_options.Database, _options.Collection);

  /// <<inheritdoc/>
  public async Task InitializeAsync(CancellationToken cancellationToken)
  {
    if (!IsAlreadyInitialized)
    {
      _logger?.LogInformation("Initializing Database");

      DatabaseResponse db = await _client.CreateDatabaseIfNotExistsAsync(_options.Database, cancellationToken: cancellationToken).ConfigureAwait(false);
      if (db.StatusCode == System.Net.HttpStatusCode.Created)
      {
        _logger?.LogInformation("Created Database {Database} in Cosmos DB", _options.Database);
      }
      ContainerResponse container = await db.Database.CreateContainerIfNotExistsAsync(_options.Collection, "/StreamId", cancellationToken: cancellationToken).ConfigureAwait(false);
      if (container.StatusCode == System.Net.HttpStatusCode.Created)
      {
        _logger?.LogInformation("Created Container {Container} in {Database}", _options.Collection, _options.Database);
      }
      IsAlreadyInitialized = true;
    }
  }
}
