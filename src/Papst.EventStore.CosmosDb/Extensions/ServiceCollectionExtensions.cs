using Azure.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.Abstractions;
using Papst.EventStore.CosmosDb.CosmosClients;
using System;

namespace Papst.EventStore.CosmosDb.Extensions;

/// <summary>
/// Extensions for the <see cref="IServiceCollection"/> to add necessary services for the <see cref="CosmosEventStore"/>
/// </summary>
public static class ServiceCollectionExtensions
{
  /// <summary>
  /// Add the Cosmos Database SQL Api EventStore
  /// </summary>
  /// <param name="services"></param>
  /// <param name="credential">The Token Credential used to connect to the Database</param>
  /// <param name="config"></param>
  /// <returns></returns>
  public static IServiceCollection AddCosmosEventStore(this IServiceCollection services, TokenCredential credential, IConfiguration config) => services
      .AddSingleton<IEventStoreCosmosClient, EventStoreCosmosClient>()
      .AddTransient<IEventStore, CosmosEventStore>()  // Add the Cosmos EventStore
      .Configure<CosmosEventStoreOptions>(c => config.Bind(c))
      .PostConfigure<CosmosEventStoreOptions>(opt => opt.Credential = credential)
      ;

  [Obsolete("Use Credential Based access instead of Shared Secrets!")]
  public static IServiceCollection AddCosmosEventStore(this IServiceCollection services, IConfiguration config) => services
      .AddSingleton<IEventStoreCosmosClient, EventStoreCosmosClient>()
      .AddTransient<IEventStore, CosmosEventStore>()  // Add the Cosmos EventStore
      .Configure<CosmosEventStoreOptions>(c => config.Bind(c))
      ;
}
