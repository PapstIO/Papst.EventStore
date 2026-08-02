# FileSystem Implementation for `Papst.EventStore`

File system based implementation of the `Papst.EventStore`. This implementation is intended for **testing and demonstration purposes only** - it is not recommended for production use.

## Usage

### High-Level Event Store (IEventStore)

```csharp
IServiceCollection services = new ServiceCollection();
var config = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string> { { "Path", "/path/to/events" } })
    .Build();

services.AddFileSystemEventStore(config.GetSection("EventStore"));

var serviceProvider = services.BuildServiceProvider();
var eventStore = serviceProvider.GetRequiredService<IEventStore>();
```

### Low-Level Event Store (ILowLevelEventStore)

For handling events as raw JSON objects without type information:

```csharp
var lowLevelEventStore = serviceProvider.GetRequiredService<ILowLevelEventStore>();
var streamId = Guid.NewGuid();
var stream = await lowLevelEventStore.CreateAsync(streamId, "MyAggregate");

// Append a low-level event (as JObject)
var evt = JObject.Parse(@"{ ""fileData"": ""content"" }");
await stream.AppendAsync(Guid.NewGuid(), "FileCreated", evt);
```

### Deleting an Event Stream

`DeleteAsync` recursively deletes the stream's directory (its index file and all event files) from disk:

```csharp
await eventStore.DeleteAsync(streamId);
```

This is a **permanent, hard delete** of the on-disk files. If the stream directory does not exist, an `EventStreamNotFoundException` is thrown. The delete action is logged at `Information` level.

## Configuration

The FileSystem implementation requires a configuration section with the path where events will be stored:

```csharp
"EventStore": {
  "Path": "/path/to/events/directory"
}
```

Events are stored as JSON files organized by stream ID in the configured directory.

## Important Notes

- This implementation stores events on the local file system
- Not suitable for production use
- Best used for testing, prototyping, and demonstration purposes
- Events are stored as JSON files; concurrent access must be managed carefully
