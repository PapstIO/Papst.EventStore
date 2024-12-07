# InMemory Implementation for the `Papst.EventStore`

This is an in-memory implementation of the `Papst.EventStore` that can be used for testing purposes.

## Usage

```csharp
IServiceCollection services = new ServiceCollection();

services.AddEventStoreInMemory();

var serviceProvider = services.BuildServiceProvider();

var eventStore = serviceProvider.GetRequiredService<IEventStore>();
```