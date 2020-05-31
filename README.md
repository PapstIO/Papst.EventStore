# Event Store

Event Souring Library that allows storing Events into a Database.
Comes with an implementation of a Azure CosmosDb EventStore.

The CosmosDb Implementation uses the following Microsoft libraries:

- `Microsoft.Azure.Cosmos`
- `Microsoft.Extensions.Configuration.Abstractions`
- `Microsoft.Extensions.Configuration.Binder`
- `Microsoft.Extensions.Logging.Abstractions`
- `Microsoft.Extensions.Options`

and comes with included compatability to the dependency injection of the .NET Core (Web)HostBuilder.

A Sample can be found in the samples directory.

## Installing the Library

- Papst.EventStore.Abstractions ![Nuget](https://img.shields.io/nuget/v/Papst.EventStore.Abstractions?style=plastic)
- Papst.EventStore.CosmosDb ![Nuget](https://img.shields.io/nuget/v/Papst.EventStore.CosmosDb?style=plastic)


## Configuring the Cosmos Db EventStore

The CosmosEventStore class uses the [Options pattern](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-3.1) to retrieve its configuration.
Therefore a ConfigurationSection can be added to the `AddCosmosEventStore` extension method for the IServiceCollection.
The C# representation of the Configuration Sections looks like this:

```csharp
    /// <summary>
    /// Configuration for the Cosmos Database Connection
    /// </summary>
    public class CosmosEventStoreOptions
    {
        /// <summary>
        /// Endpoint URL
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Secret Identifier
        /// </summary>
        public string AccountSecret { get; set; }

        /// <summary>
        /// Whether to try to create Database and collection after creation of the Client
        /// </summary>
        public bool InitializeOnStartup { get; set; }

        /// <summary>
        /// Name of the Collection
        /// </summary>
        public string Collection { get; set; }

        /// <summary>
        /// Name of the Database
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Whether to allow a Event Timestamp to be set from the outside or 
        /// to override the time before inserting the document
        /// </summary>
        public bool AllowTimeOverride { get; set; }
    }
```

## Testing

Unfortunately Unit Tests are still a ToDo.