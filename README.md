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

## Attribute-based Event Aggregation

In addition to hand-written aggregators (deriving from `EventAggregatorBase<TEntity, TEvent>`), the code
generator can **generate the aggregator for you** from a single attribute on the event. This is **opt-in** and
works **in parallel** with the classic API — you can mix generated and hand-written aggregators freely, and an
event only gets a generated aggregator when it is annotated.

Annotate an event with `[EventAggregation<TEntity>]`. The generator emits an `IEventAggregator<TEntity, TEvent>`
that copies the event's properties onto the target entity by **matching property name**:

```csharp
[EventName<Order>("OrderShipped")]
[EventAggregation<Order>]
public sealed record OrderShippedEvent(
  OrderStatus Status,
  string DeliveryTrackingCode,
  DateTimeOffset PickupDate,
  DateTimeOffset EstimatedArrivalDate);
```

Every event property that has a settable, equally named property on the entity is assigned. As with any
generated event, the event still needs an `EventName` for type resolution, and the generated aggregator is
registered by the existing `AddCodeGeneratedEvents()` extension.

### Ignoring and renaming properties

Use `[AggregationIgnore]` to exclude a property from aggregation, and `[AggregationProperty("TargetName")]` to
map a property onto a differently named property on the entity:

```csharp
[EventAggregation<Order>]
public sealed record OrderRenamed(
  [property: AggregationProperty(nameof(Order.CustomerName))] string DisplayName, // written to Order.CustomerName
  [property: AggregationIgnore] string CorrelationId                             // never mapped
);
```

### Null handling

By default `null` event values are **skipped** (the existing entity value is kept). Set
`SkipNullValues = false` on the attribute to always write the value (including `null`), or override the
behaviour for a single property with `[SkipWhenNull(bool)]`:

```csharp
[EventAggregation<Order>(SkipNullValues = false)]
public sealed record OrderContactChanged(
  string? Email,                              // written even when null
  [property: SkipWhenNull(true)] string? Note // kept when null
);
```

### Nested targets via `PropertyPath`

`PropertyPath` selects a nested object on the entity as the aggregation target (dot-separated). Intermediate
`null` links are instantiated automatically:

```csharp
[EventAggregation<Order>(PropertyPath = nameof(Order.ShippingAddress))]
public sealed record ShippingAddressSet(string? City, string? Zip);
```

### Dictionaries and collections

When `PropertyPath` points at a dictionary or collection, mark the event property that identifies the entry.
Missing entries are created and added (**upsert**):

```csharp
// Dictionary<string, OrderLine> Lines — upsert the entry under the given key
[EventAggregation<Order>(PropertyPath = nameof(Order.Lines))]
public sealed record LineUpserted([property: AggregationDictionaryKey] string Sku, int Quantity);

// List<OrderTag> Tags — upsert the item whose Id equals the event value
[EventAggregation<Order>(PropertyPath = nameof(Order.Tags))]
public sealed record TagUpserted([property: AggregationCollectionKey("Id")] string TagId, string? Label);
```

A working example lives in the Orders module of
[`samples/SampleInMemoryAspNetApi/`](./samples/SampleInMemoryAspNetApi/) (`OrderShippedEvent`).

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

This API is implemented by all EventStore providers (Azure Cosmos, Entity Framework Core, MongoDB, FileSystem and InMemory).

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

## Deleting an Event Stream

The `IEventStore` interface offers a `DeleteAsync` method that permanently removes an event stream together with **all** of its documents (events, snapshots and the stream index):

```csharp
Task DeleteAsync(
    Guid streamId,
    CancellationToken cancellationToken = default);
```

```csharp
await eventStore.DeleteAsync(streamId, cancellationToken);
```

Semantics:

* Deletion is **store-level** — it removes the entire stream identified by `streamId`.
* If the stream does not exist, an `EventStreamNotFoundException` is thrown.
* The deletion is **permanent and irreversible**. Every provider hard-deletes the underlying data (dictionary entry, database rows/documents or files) — there is no soft-delete or tombstone.

The action is implemented and logged by every provider (Azure Cosmos, Entity Framework Core, MongoDB, FileSystem and InMemory). See the provider READMEs for provider-specific details.

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

## V 7.0

Adds an opt-in, attribute-based way to generate event aggregators, usable in parallel with the existing
hand-written aggregator API.

### Changes

* New `[EventAggregation<TEntity>]` attribute on an event class/record makes the
  `Papst.EventStore.CodeGeneration` source generator emit the aggregator and register it via
  `AddCodeGeneratedEvents()`. Event properties are mapped onto the entity by name.
* `PropertyPath` targets a nested object (intermediate `null` links are instantiated), a `Dictionary<,>`
  (mark the key with `[AggregationDictionaryKey]`) or a collection (mark the search key with
  `[AggregationCollectionKey("Id")]`); missing dictionary/collection items are upserted.
* Null handling is controlled globally by `SkipNullValues` (default `true`) and per property by
  `[SkipWhenNull(bool)]`.
* Individual event properties can be excluded with `[AggregationIgnore]` or mapped onto a differently named
  entity property with `[AggregationProperty("TargetName")]`.
* If a hand-written aggregator already exists for the same `(entity, event)` pair, generation is skipped and
  an `EVTSRC0003` diagnostic is reported, so the two approaches never double-register.
* The code generator no longer fails when a project contains nested event/aggregator types; such types are
  skipped by the (name-based) discovery instead.

## V 6.4

Aligns the event version numbering across all stores and lets the aggregator target the creation event.

### Changes

* The `InMemory` and `MongoDB` stores now number the **first event of a stream `0`**, matching the
  Azure Cosmos, Entity Framework Core and FileSystem stores. Previously they were 1-based, which made
  `InMemory` an unreliable stand-in for Cosmos in tests (see
  [#282](https://github.com/PapstIO/Papst.EventStore/issues/282)).
* `EventRegistrationEventAggregator` can now aggregate up to and including version `0` (the creation
  event on 0-based stores). The stop condition compares the event version instead of the aggregate
  version, so `AggregateAsync(stream, 0, ct)` stops after the creation event instead of replaying the
  whole stream.
* **Breaking (MongoDB):** existing persisted MongoDB streams created with an earlier version are 1-based;
  their metadata gains a `NextVersion` field that defaults to `0`. Streams created before upgrading need
  to be migrated (or recreated) before appending further events.

## V 6.3

Adds the ability to delete an entire event stream.

### Changes

* Adds `IEventStore.DeleteAsync(Guid streamId, CancellationToken)` which permanently removes a stream and all of its documents.
* Implemented and logged across all providers (Azure Cosmos, Entity Framework Core, MongoDB, FileSystem and InMemory).
* Deleting a non-existent stream throws `EventStreamNotFoundException`.

## V 6.1

Maintenance release for the V6 package line.

### Changes

* Updates the EventStore implementation packages to depend on `Papst.EventStore` `>= 6.1.0` and `< 7.0.0`.
* Adds `ILowLevelEventStream` API

## V 6.0

V6 introduces the Event Catalog and low-level event access APIs.

### Changes

* Adds the `IEventCatalog` abstraction for querying registered events and generated JSON schema metadata.
* Extends code generation with `AddCodeGeneratedEventCatalog()`.
* Adds `ILowLevelEventStream` support for appending raw `JObject` payloads together with explicit event type names.

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

