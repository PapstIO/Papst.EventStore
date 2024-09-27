# Azure Cosmos Implementation of Event Stream

The Azure Cosmos Implementation needs to be added to the dependency injection container.

To achieve this, there are two extension methods available, one using a client factory function and one a direct `CosmosClient` parameter:
```csharp
IServiceCollection services;

// direct CosmosClient parameter
services.AddCosmosEventStore(
    new CosmosClient(...),
    "database",
    "container");

// CosmosClient Factory, that has a IServiceProvider as parameter
services.AddCosmosEventStore(
    sp => sp.GetRequiredService<CosmosClient>(),
    "database",
    "container");
```

In addition it is necessary to Configure the class `CosmosEventStoreOptions` with the options below.

## Configuration

The CosmosEventStore class uses the [Options pattern](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-3.1) to retrieve its configuration.
Therefore a ConfigurationSection can be added to the `AddCosmosEventStore` extension method for the IServiceCollection.
The C# representation of the Configuration Sections looks like this:

```csharp
public class CosmosEventStoreOptions
{
  /// <summary>
  /// Count of retries when two parties are updating the same stream to avoid concurrency issues
  /// </summary>
  public int ConcurrencyRetryCount { get; set; } = 3;
  
  /// <summary>
  /// Whether to try to build the index for an existing stream, when no index is found
  /// or to return a <see cref="Papst.EventStore.Exceptions.EventStreamNotFoundException"/>
  /// </summary>
  public bool BuildIndexOnNotFound { get; set; }
  
  /// <summary>
  /// Flag if the Tenant Id of the Stream Meta Data should be updated when appending a new event
  /// </summary>
  public bool UpdateTenantIdOnAppend { get; set; } = true;
}
```

## Migration

With the new V5 based implementation, there is need for an index document in the cosmos database for each stream.

For a convenient upgrade, there has been an option to the configuration added, that builds an index, when a stream is requested using `GetAsync` method in `IEventStore` and the stream is not found.

A manual upgrade is not yet supported.