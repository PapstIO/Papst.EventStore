using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.CosmosDb;
using Xunit;

namespace Papst.EventStore.AzureCosmos.Tests;

public class CosmosDbIntegrationTestFixture : IAsyncLifetime
{
  public string DatabaseName => CosmosDatabaseName;
  public string ContainerName => CosmosContainerId;

  private readonly CosmosDbContainer _cosmosDbContainer = new CosmosDbBuilder()
    .WithImage("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest")
    .WithPortBinding(8081, 8081)
    .WithEnvironment("AZURE_COSMOS_EMULATOR_PARTITION_COUNT", "10")
    .WithEnvironment("AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTANCE", "false")
    .WithAutoRemove(true)
    .Build();
  private CosmosClient? _cosmosClient;

  public const string CosmosContainerId = "Events";

  public const string CosmosDatabaseName = "EventStore";

  public async Task InitializeAsync()
  {
    await _cosmosDbContainer.StartAsync();

    _cosmosClient = new CosmosClient(_cosmosDbContainer.GetConnectionString(), new CosmosClientOptions
    {
      HttpClientFactory = () => _cosmosDbContainer.HttpClient,
      ConnectionMode = ConnectionMode.Gateway
    });

    var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseName);
    await database.Database.CreateContainerIfNotExistsAsync(ContainerName, "/StreamId");
  }

  public IServiceProvider BuildServiceProvider(Action<IServiceCollection>? configureServices = null)
  {
    if (_cosmosClient == null)
    {
      throw new NotSupportedException();
    }

    var services = new ServiceCollection();
    services.AddLogging();

    services.AddCosmosEventStore(_cosmosClient, CosmosDatabaseName, CosmosContainerId);
    services.AddCodeGeneratedEvents();
    services.AddSingleton(_cosmosClient);

    configureServices?.Invoke(services);

    return services.BuildServiceProvider();
  }

  public async Task DisposeAsync()
  {
    _cosmosClient?.Dispose();

    await _cosmosDbContainer.StopAsync();
    await _cosmosDbContainer.DisposeAsync();
  }
}
