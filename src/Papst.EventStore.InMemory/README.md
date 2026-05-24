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