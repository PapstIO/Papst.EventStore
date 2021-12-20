using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Papst.EventStore.CosmosDb.CosmosClients;

internal abstract class EventStoreCosmosClientBase : IEventStoreCosmosClient
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

  protected EventStoreCosmosClientBase(
      ILogger logger,
      CosmosClient client,
      CosmosEventStoreOptions options)
  {
    _logger = logger;
    _client = client;
    _options = options;
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
