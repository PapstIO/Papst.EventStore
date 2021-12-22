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

## Version

### v3.x

V3 supports mainly .NET 5.0 and registration of events and event aggregators through reflection

### v4

* V4 only supports .NET 6.0

It introduces the concept of Code Generated registration of events and aggregators by decorating them with the `EventName` attribute.

It also decouples the auto generated event type descriptor (was basically a description used to revert to a type using `Type.GetType()`) from the concrete implementation.
This allows to version and migrate events by just adding a different descriptor.

A sample on how to use the Event Descriptors is found under [Samples](samples/SampleCodeGeneratedEvents/Program.cs). The Extension Method `AddCodeGeneratedEvents()` is automatically generated during compilation if the package `Papst.EventStore.CodeGeneration` is added to the Project.

Migrate v3 events by adding a `EventName` attribute and add the typename as a name: `[Fullename of the type],[assembly name of the type]`.
For the `MyEventSourcingEvent` in the [Code Generation Sample](samples/SampleCodeGeneratedEvents/Program.cs) it would look like this:

`SampleCodeGeneratedEvents.MyEventSourcingEvent,SampleCodeGeneratedEvents`

#### Breaking Change

V4 removes support for authenticating with shared keys against the cosmos DB. The implementation is still there, but changed and marked as obsolete.

## Installing the Library

- [Papst.EventStore.Abstractions](https://www.nuget.org/packages/Papst.EventStore.Abstractions/) ![Nuget](https://img.shields.io/nuget/v/Papst.EventStore.Abstractions?style=plastic)
- [Papst.EventStore.CosmosDb](https://www.nuget.org/packages/Papst.EventStore.CosmosDb/) ![Nuget](https://img.shields.io/nuget/v/Papst.EventStore.CosmosDb?style=plastic)
- [Papst.EventStore.CodeGeneration](https://www.nuget.org/packages/Papst.EventStore.CodeGeneration/) ![Nuget](https://img.shields.io/nuget/v/Papst.EventStore.CodeGeneration?style=plastic)

## Configuring the Code Generator

- Events must be attributed with the `EventName` attribute.
- EventAggregators, implementing the interface `IEventAggregator` or implementing the abstract class `EventAggregatorBase` are automatically added to the Dependency Injection.

### Configuring Write and Read Event Identifier

It is possible to have multiple `EventName` attributes on just one event. With the attribute `IsWriteName` it is possible to define which Identifier is used when writing the event.
```csharp
[EventName(Name = "MyEventV1", IsWriteName = false)]
[EventName(Name = "MyEventV2")]
public class MyEventsourcingEvent 
{

}
```
Reading Events named `MyEventV1` or `MyEventV2` will deserialize them into a `MyEventsourcingEvent`.
Writing `MyEventsourcingEvent` to the Event Stream, will serialize them and name them `MyEventV2`.

Note: `IsWriteName` is true by default!

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

    /// <summary>
    /// Credential which is used to Authenticate agains the Database
    /// </summary>
    public TokenCredential? Credential { get; internal set; }
}
```
