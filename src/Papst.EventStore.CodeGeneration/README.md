# Papst.EventStore.CodeGeneration

Roslyn source generator for [Papst.EventStore](https://github.com/PapstIO/Papst.EventStore).

The generator inspects your compilation at build time and emits the dependency injection registration
for your events and aggregators, so you never hand-maintain a registry of event names or handler types.

## Installation

```
dotnet add package Papst.EventStore.CodeGeneration
```

This package ships as an analyzer. It contributes no runtime dependencies to your application.

## What it generates

Decorate your events and aggregators:

```csharp
[EventName("OrderPlaced")]
public record OrderPlaced(Guid OrderId, decimal Total);
```

The generator emits `AddCodeGeneratedEvents()` — an `IServiceCollection` extension that registers every
discovered event name to its CLR type, registers the discovered `IEventAggregator<,>` implementations,
and wires up an `IEventTypeProvider` if none is registered yet:

```csharp
services.AddCodeGeneratedEvents();
```

It also emits `AddCodeGeneratedEventCatalog()`, registering an `IEventCatalog` describing the known events.

Multiple `[EventName]` attributes on one type are supported for event versioning; mark legacy names with
`IsWriteName = false` to keep them readable without writing them.

See the [project README](https://github.com/PapstIO/Papst.EventStore) for the full documentation.

## License

MIT
