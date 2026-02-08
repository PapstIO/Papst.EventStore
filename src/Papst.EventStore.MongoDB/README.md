# Papst.EventStore.MongoDB

MongoDB based EventStore Implementation.

## Usage

```csharp
services.AddMongoDBEventStore(options => {
    options.ConnectionString = "mongodb://localhost:27017";
    options.DatabaseName = "EventStore";
});
```

## Features

- Full IEventStore and IEventStream interface implementation
- MongoDB native async operations
- Transaction support for batch operations
- Efficient querying with MongoDB indexes
- Snapshot support
