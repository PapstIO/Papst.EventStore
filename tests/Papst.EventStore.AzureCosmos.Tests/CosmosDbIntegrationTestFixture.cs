using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.Aggregation.EventRegistration;
using Testcontainers.CosmosDb;
using Xunit;

namespace Papst.EventStore.AzureCosmos.Tests;

public class CosmosDbIntegrationTestFixture : IAsyncLifetime
{
  public string DatabaseName => CosmosDatabaseName;
  public string ContainerName => CosmosContainerId;

  private const ushort InternalPort = 8081;

  private readonly CosmosDbContainer _cosmosDbContainer =
    //new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview")
    new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest")
      .WithPortBinding(InternalPort, true)
      //.WithEnvironment("AZURE_COSMOS_EMULATOR_PARTITION_COUNT", "10")
      .WithEnvironment("AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTANCE", "false")
      //.WithCommand("--protocol", "https")
      .WithAutoRemove(true)
      //.WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new WaitUntil()))
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
      ConnectionMode = Microsoft.Azure.Cosmos.ConnectionMode.Gateway
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
    services.AddRegisteredEventAggregation();
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

  private sealed class WaitUntil : IWaitUntil
  {
    private readonly HttpClient _client;

    public WaitUntil()
    {
      var handler = new HttpClientHandler();
      handler.ClientCertificateOptions = ClientCertificateOption.Manual;
      handler.ServerCertificateCustomValidationCallback =
          (httpRequestMessage, cert, cetChain, policyErrors) =>
          {
            return true;
          };
      _client = new HttpClient(handler);
    }
    public async Task<bool> UntilAsync(IContainer container)
    {
      // CosmosDB's preconfigured HTTP client will redirect the request to the container.

      bool foundPort = container.GetMappedPublicPorts().TryGetValue(InternalPort, out ushort port);

      string requestUri = "https://localhost" + (foundPort ? $":{port}" : "");
      //var httpClient = ((CosmosDbContainer)container).HttpClient;

      try
      {
        using var httpResponse = await _client.GetAsync(requestUri).ConfigureAwait(false);

        return httpResponse.IsSuccessStatusCode;
      }
      catch (Exception)
      {
        return false;
      }
      finally
      {
        //httpClient.Dispose();
      }
    }
  }
}
