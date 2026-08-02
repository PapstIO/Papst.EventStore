# InMemory Implementation for the `Papst.EventStore`

This is an in-memory implementation of the `Papst.EventStore` that can be used for testing purposes.

## Usage

### High-Level Event Store (IEventStore)

```csharp
IServiceCollection services = new ServiceCollection();

services.AddInMemoryEventStore();

var serviceProvider = services.BuildServiceProvider();

var eventStore = serviceProvider.GetRequiredService<IEventStore>();
```

### Low-Level Event Store (ILowLevelEventStore)

For handling events as raw JSON objects without type information:

```csharp
IServiceCollection services = new ServiceCollection();

services.AddInMemoryEventStore();

var serviceProvider = services.BuildServiceProvider();

var lowLevelEventStore = serviceProvider.GetRequiredService<ILowLevelEventStore>();

// Create a stream
var streamId = Guid.NewGuid();
var stream = await lowLevelEventStore.CreateAsync(streamId, "MyAggregate");

// Append a low-level event (as JObject)
var evt = JObject.Parse(@"{ ""name"": ""John"", ""age"": 30 }");
await stream.AppendAsync(Guid.NewGuid(), "UserCreated", evt);
```

### Deleting an Event Stream

`DeleteAsync` removes the stream from the in-memory dictionary. Because the InMemory store is not persisted, this simply drops the stream and all of its events from memory:

```csharp
await eventStore.DeleteAsync(streamId);
```

If the stream does not exist, an `EventStreamNotFoundException` is thrown. The delete action is logged (`Information` level) via the store's `ILogger`.