using Azure.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.Abstractions;
using Papst.EventStore.CosmosDb.CosmosClients;

namespace Papst.EventStore.CosmosDb.Extensions;

/// <summary>
/// Extensions for the <see cref="IServiceCollection"/> to add necessary services for the <see cref="CosmosEventStore"/>
/// </summary>
public static class ServiceCollectionExtensions
{
  /// <summary>
  /// Add the Cosmos Database SQL Api EventStore
  /// Note: Using a Shared Secret to connect to Cosmos Database
  /// </summary>
  /// <param name="services"></param>
  /// <param name="config"></param>
  /// <returns></returns>
  public static IServiceCollection AddCosmosEventStore(this IServiceCollection services, IConfiguration config) => services
      .AddSingleton<IEventStoreCosmosClient, SharedSecretCosmosClient>()
      .AddCosmosServices(config)
      ;

  /// <summary>
  /// Add the Cosmos Database SQL Api EventStore
  /// </summary>
  /// <param name="services"></param>
  /// <param name="credential">The Token Credential used to connect to the Database</param>
  /// <param name="config"></param>
  /// <returns></returns>
  public static IServiceCollection AddCosmosEventStore(this IServiceCollection services, TokenCredential credential, IConfiguration config) => services
      .AddSingleton(new ManagedIdentityCosmosClientCredential(credential))
      .AddSingleton<IEventStoreCosmosClient, ManagedIdentityCosmosClient>()
      .AddCosmosServices(config)
      ;

  /// <summary>
  /// Internal: adds necessary services
  /// </summary>
  /// <param name="services"></param>
  /// <returns></returns>
  private static IServiceCollection AddCosmosServices(this IServiceCollection services, IConfiguration config) => services
      .AddTransient<IEventStore, CosmosEventStore>()  // Add the Cosmos EventStore
      .Configure<CosmosEventStoreOptions>(c => config.Bind(c))

      ;
}
