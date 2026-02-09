using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Papst.EventStore.MongoDB;

public static class MongoDBEventStoreProvider
{
  /// <summary>
  /// Add the MongoDB EventStore to the DI Container
  /// </summary>
  /// <param name="services">Service collection</param>
  /// <param name="configureOptions">Action to configure MongoDB options</param>
  /// <returns>Service collection for chaining</returns>
  public static IServiceCollection AddMongoDBEventStore(
    this IServiceCollection services,
    Action<MongoDBEventStoreOptions> configureOptions)
  {
    services.Configure(configureOptions);
    
    // Add PostConfigure validation
    services.PostConfigure<MongoDBEventStoreOptions>(options =>
    {
      if (string.IsNullOrWhiteSpace(options.ConnectionString))
      {
        throw new InvalidOperationException("MongoDB ConnectionString must be configured.");
      }
      
      if (string.IsNullOrWhiteSpace(options.DatabaseName))
      {
        throw new InvalidOperationException("MongoDB DatabaseName must be configured.");
      }
      
      if (string.IsNullOrWhiteSpace(options.CollectionName))
      {
        options.CollectionName = "EventStreams";
      }
      
      if (string.IsNullOrWhiteSpace(options.StreamMetadataCollectionName))
      {
        options.StreamMetadataCollectionName = "StreamMetadata";
      }
    });
    
    services.AddSingleton<IEventStore, MongoDBEventStore>();
    services.TryAddSingleton(TimeProvider.System);
    return services;
  }

  /// <summary>
  /// Add the MongoDB EventStore to the DI Container with connection string
  /// </summary>
  /// <param name="services">Service collection</param>
  /// <param name="connectionString">MongoDB connection string</param>
  /// <param name="databaseName">Database name</param>
  /// <returns>Service collection for chaining</returns>
  public static IServiceCollection AddMongoDBEventStore(
    this IServiceCollection services,
    string connectionString,
    string databaseName = "EventStore")
  {
    return AddMongoDBEventStore(services, options =>
    {
      options.ConnectionString = connectionString;
      options.DatabaseName = databaseName;
    });
  }
}
