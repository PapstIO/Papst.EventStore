using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Papst.EventStore.AzureCosmos.Database;

namespace Papst.EventStore.AzureCosmos;

public static class CosmosEventStoreProvider
{
  /// <summary>
  /// Add the Cosmos Database EventStore to the DI Container
  /// </summary>
  /// <param name="services"></param>
  /// <param name="client">The Cosmos Database Client</param>
  /// <param name="databaseId">The Name of the Database to be used</param>
  /// <param name="containerId">The Name of the Container to be used</param>
  /// <returns></returns>
  public static IServiceCollection AddCosmosEventStore(
    this IServiceCollection services,
    CosmosClient client,
    string databaseId,
    string containerId
  ) => services.AddCosmosEventStore(_ => client, databaseId, containerId);

  /// <summary>
  /// Add the Cosmos Database EventStore to the DI Container
  /// </summary>
  /// <param name="services"></param>
  /// <param name="clientFactoryFunction">The <see cref="CosmosClient"/> factory method</param>
  /// <param name="databaseId">The Name of the Database to be used</param>
  /// <param name="containerId">The Name of the Container to be used</param>
  /// <returns></returns>
  public static IServiceCollection AddCosmosEventStore(
    this IServiceCollection services,
    Func<IServiceProvider, CosmosClient> clientFactoryFunction,
    string databaseId,
    string containerId
  ) => services
    .AddTransient<IEventStore, CosmosEventStore>()
    .AddSingleton<ICosmosIdStrategy, StreamIdEventTypeIdStrategy>()
    .AddSingleton(
      provider => new CosmosDatabaseProvider(
        provider.GetRequiredService<ILogger<CosmosDatabaseProvider>>(),
        clientFactoryFunction(provider),
        databaseId,
        containerId
      ));


  /// <summary>
  /// Register a custom Id strategy for events
  /// </summary>
  /// <param name="services"></param>
  /// <typeparam name="TStrategy">The Id Strategy implementation</typeparam>
  /// <returns></returns>
  public static IServiceCollection UseCosmosEventStoreStreamIdStrategy<TStrategy>(this IServiceCollection services)
    where TStrategy : class, ICosmosIdStrategy => services
    .RemoveAll(typeof(ICosmosIdStrategy))
    .AddSingleton<ICosmosIdStrategy, TStrategy>();
}
