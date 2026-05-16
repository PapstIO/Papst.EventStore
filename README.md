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

## Available EventStore implementations

The library brings a couple of already implemented EventStore packages:

* [Papst.EventStore.FileSystem](https://www.nuget.org/packages/Papst.EventStore.FileSystem/) ![Nuget](https://img.shields.io/nuget/v/Papst.EventStore.FileSystem?style=plastic)  
  **Note**: This is for testing purpose only!
* [Papst.EventStore.EntityFrameworkCore](https://www.nuget.org/packages/Papst.EventStore.EntityFrameworkCore/) ![Nuget](https://img.shields.io/nuget/v/Papst.EventStore.EntityFrameworkCore?style=plastic)
* [Papst.EventStore.AzureCosmos](https://www.nuget.org/packages/Papst.EventStore.AzureCosmos/) ![Nuget](https://img.shields.io/nuget/v/Papst.EventStore.AzureCosmos?style=plastic)
* [Papst.EventStore.MongoDB](https://www.nuget.org/packages/Papst.EventStore.AzureBlob/) ![Nuget](https://img.shields.io/nuget/v/Papst.EventStore.MongoDB?style=plastic)

## Installing the Library

- [Papst.EventStore](https://www.nuget.org/packages/Papst.EventStore/) ![Nuget](https://img.shields.io/nuget/v/Papst.EventStore?style=plastic)
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

## Configuring an Implementation for use

Please refer to the documentation in the relevant implementation sources:

* [Azure Cosmos](./src/Papst.EventStore.AzureCosmos/README.md)
* [Entity Framework Core](./src/Papst.EventStore.EntityFrameworkCore/README.md)

## Low-level event access

The `ILowLevelEventStream` interface provides a low-level append API that accepts a raw `JObject` together with an explicit event type name:

```csharp
Task AppendAsync(
    Guid id,
    string eventType,
    JObject evt,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default);
```

This API is currently only implemented by the Azure Cosmos provider.

`ILowLevelEventStream` is not resolved directly from `IEventStore`. Instead, obtain an `IEventStream` first and then cast it to `ILowLevelEventStream` when the underlying provider supports it:

```csharp
IEventStream stream = await eventStore.GetAsync(streamId, cancellationToken);

if (stream is ILowLevelEventStream lowLevelStream)
{
    await lowLevelStream.AppendAsync(
        Guid.NewGuid(),
        "MyExternalEvent",
        JObject.Parse("""{ \"value\": 42 }"""),
        cancellationToken: cancellationToken);
}
```

This is intended for scenarios where the event payload or event type is not known at compile time. If you work with strongly typed events, prefer `IEventStream.AppendAsync<TEvent>()`.

## Event Catalog

The **Event Catalog** provides a queryable registry of all events associated with a given entity type, including metadata (description, constraints) and a compile-time generated JSON Schema. This is useful for documentation, API discovery, and runtime introspection.

### Registering Events for the Catalog

Use the generic `EventNameAttribute<TEntity>` to associate an event with an entity:

```csharp
[EventName<User>("UserCreated", Description = "Raised when a new user is created", Constraints = new[] { "Create" })]
public record UserCreatedEvent(string Name, string Email);

[EventName<User>("UserRenamed", Description = "Raised when a user changes their name", Constraints = new[] { "Update" })]
public record UserRenamedEvent(string NewName);
```

Events can also be discovered automatically via aggregator registrations (`EventAggregatorBase<TEntity, TEvent>` / `IEventAggregator<TEntity, TEvent>`).

The same event name may exist for different entity types — the catalog tracks them independently per entity.

### Code Generation

When the `Papst.EventStore.CodeGeneration` package is referenced, the source generator automatically emits an `AddCodeGeneratedEventCatalog()` extension method alongside the existing `AddCodeGeneratedEvents()`:

```csharp
var services = new ServiceCollection();
services.AddCodeGeneratedEvents();
services.AddCodeGeneratedEventCatalog();
```

### Querying the Catalog

Resolve `IEventCatalog` from DI and use the async API:

```csharp
var catalog = serviceProvider.GetRequiredService<IEventCatalog>();

// List all events for an entity
IReadOnlyList<EventCatalogEntry> events = await catalog.ListEvents<User>();

// Filter by name and/or constraints
IReadOnlyList<EventCatalogEntry> filtered = await catalog.ListEvents<User>(
    name: "UserCreated",
    constraints: new[] { "Create" }
);

// Get event details including JSON Schema (global lookup)
EventCatalogEventDetails? details = await catalog.GetEventDetails("UserCreated");

// Get event details scoped to a specific entity (for duplicate event names across entities)
EventCatalogEventDetails? scoped = await catalog.GetEventDetails<User>("UserCreated");
```

A full working sample is available at [`samples/SampleEventCatalog/`](./samples/SampleEventCatalog/).
For an end-to-end ASP.NET Core example using the in-memory event store, stream aggregation, and read-model repositories, see [`samples/SampleInMemoryAspNetApi/`](./samples/SampleInMemoryAspNetApi/).

# Changelog

## V 5.4

Adds a possibility to update a streams MetaData object.

## V 5.3

V5.3 introduces new methods on the `IAggregatorStreamContext` that allows to transfer information from the Aggregator to the next one.

**V5.3 Supports only .NET 10.0 and upwards**

## V 5.2

V5.2 introduces Metadata on the Stream itself. The `IEventStream` now got its own metadata Property.

Meta Data for the Stream needs to be set during creation, otherwise it will be empty. To Create a stream with Meta Data the `IEventStore`has got a new extended `CreateAsync` method that takes the additional metadata.

Only the Azure Cosmos Implementation offers a new option in the configuration that updates the TenantId based on the last set event.

## V 5 / V5.1

V5 comes with a new access model to the streams, with paging and a new library structure.

**V5 Supports only .NET 8.0 and upwards**

It introduces a separation of EventStore and EventStream. The EventStore now only offers the possibility to create or retrieve streams.

### Breaking Changes

* The `IEventStore` interface no longer has methods to append to the EventStream
* The new `IEventStream` needs an index document, which needs to be added to existing event streams. See Migration Chapter in Cosmos DB Implementation.
* The `IEventStreamAggregator` implementation that uses the code generated events has moved to a own package to allow removing active code from the `Papst.EventStore` package.
* The `EventName` Attribute now uses positional parameters, provided by a constructor.
* A single EventStream can no longer contain Events for multiple Entities.
* The `IEventStreamAggregator` now uses `ValueTask` instead of `Task`

### Changes

* Meta Data Properties are now of type `string?` instead of `Guid?` to achieve greater compatability.

## V 4

* V4 only supports .NET 6.0

It introduces the concept of Code Generated registration of events and aggregators by decorating them with the `EventName` attribute.

It also decouples the auto generated event type descriptor (was basically a description used to revert to a type using `Type.GetType()`) from the concrete implementation.
This allows to version and migrate events by just adding a different descriptor.

A sample on how to use the Event Descriptors is found under [Samples](samples/SampleCodeGeneratedEvents/Program.cs). The Extension Method `AddCodeGeneratedEvents()` is automatically generated during compilation if the package `Papst.EventStore.CodeGeneration` is added to the Project.

Migrate v3 events by adding a `EventName` attribute and add the typename as a name: `[Fullename of the type],[assembly name of the type]`.
For the `MyEventSourcingEvent` in the [Code Generation Sample](samples/SampleCodeGeneratedEvents/Program.cs) it would look like this:

`SampleCodeGeneratedEvents.MyEventSourcingEvent,SampleCodeGeneratedEvents`

### Breaking Change

V4 removes support for authenticating with shared keys against the cosmos DB. The implementation is still there, but changed and marked as obsolete.

## v3.x

V3 supports mainly .NET 5.0 and registration of events and event aggregators through reflection

