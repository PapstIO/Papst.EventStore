# Entity Framework Core Implementation of Event Stream

The Entity Framework Implementation needs to be added to the dependency injection container.

To achieve this, there is an extension method to the `IServiceCollection` available, that takes a Configuration action for the `DbContextOptionsBuilder`:

```csharp
IServiceCollection services;

services.AddEntityFrameworkCoreEventStore(options => options.AddSqlServer("..."));
```

## Usage

### High-Level Event Store (IEventStore)

```csharp
var eventStore = serviceProvider.GetRequiredService<IEventStore>();
var streamId = Guid.NewGuid();
var stream = await eventStore.CreateAsync(streamId, "MyAggregate");
```

### Low-Level Event Store (ILowLevelEventStore)

For handling events as raw JSON objects without type information:

```csharp
var lowLevelEventStore = serviceProvider.GetRequiredService<ILowLevelEventStore>();
var streamId = Guid.NewGuid();
var stream = await lowLevelEventStore.CreateAsync(streamId, "MyAggregate");

// Append a low-level event (as JObject)
var evt = JObject.Parse(@"{ ""productId"": ""ABC123"", ""quantity"": 5 }");
await stream.AppendAsync(Guid.NewGuid(), "ItemAdded", evt);
```

## Configuration

No further configuration is necessary beyond the event store registration.

## Migrations / Table creation

Tables are created using Entity Framework Core Migrations.
These Migrations are not applied automatically. This needs to be done by additional code or by applying the .sql files in the `Migrations` directory of the Source Code Repository.
