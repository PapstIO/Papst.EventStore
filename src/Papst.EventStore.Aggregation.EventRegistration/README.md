# Papst.EventStore.Aggregation.EventRegistration

Aggregation for [Papst.EventStore](https://github.com/PapstIO/Papst.EventStore) driven by code-generated
event registration.

This package provides the `IEventStreamAggregator<TEntity>` implementation that replays an event stream
onto an entity by resolving the matching `IEventAggregator<TEntity, TEvent>` handlers from the dependency
injection container. It also contains the attributes the source generator in
`Papst.EventStore.CodeGeneration` looks for.

## Installation

```
dotnet add package Papst.EventStore.Aggregation.EventRegistration
```

## Usage

Register the aggregator alongside the generated event registration:

```csharp
services.AddRegisteredEventAggregation();
services.AddCodeGeneratedEvents();   // emitted by Papst.EventStore.CodeGeneration
```

Then aggregate a stream:

```csharp
var aggregator = provider.GetRequiredService<IEventStreamAggregator<MyEntity>>();
MyEntity? entity = await aggregator.AggregateAsync(stream, cancellationToken);
```

## Attributes

| Attribute | Purpose |
|---|---|
| `[EventName("...")]` | Names an event for the registry; repeatable for versioning, set `IsWriteName = false` for read-only aliases |
| `[EventAggregation]` | Marks a type as taking part in generated aggregation |
| `[AggregationProperty]` / `[AggregationIgnore]` | Control which properties are aggregated |
| `[AggregationCollectionKey]` / `[AggregationDictionaryKey]` | Identify items when aggregating collections |
| `[SkipWhenNull]` | Skip applying a property when the event value is null |

See the [project README](https://github.com/PapstIO/Papst.EventStore) for the full documentation.

## License

MIT
