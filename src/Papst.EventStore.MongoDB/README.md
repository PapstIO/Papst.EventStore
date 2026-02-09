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
- Transaction support for batch operations (with automatic fallback for standalone instances)
- Efficient querying with MongoDB indexes
- Snapshot support
- Comprehensive logging using LoggerMessage source generator

## Architecture

### Separate Metadata Collection

The MongoDB implementation uses **two collections** to store event stream data:

1. **EventStreams Collection** - Stores individual event documents (events and snapshots)
2. **StreamMetadata Collection** - Stores metadata for each stream

#### Why Separate Collections?

**Performance Benefits:**
- **Faster Stream Queries**: The metadata collection stores a small document per stream (StreamId, Version, Created, TargetType, etc.), enabling instant retrieval of stream information without scanning through potentially thousands of events.
- **Optimized Version Tracking**: Updating the current version of a stream requires only updating a single small document in the metadata collection, rather than querying all events to determine the latest version.
- **Efficient Index Usage**: The metadata collection has a unique index on StreamId, ensuring O(1) lookups for stream existence checks and metadata retrieval.

**Data Integrity:**
- **Atomic Stream Creation**: The unique index on StreamId in the metadata collection provides database-level enforcement of stream uniqueness, preventing race conditions during concurrent stream creation.
- **Version Consistency**: Storing the current version in metadata ensures consistent version tracking even when events are being appended concurrently.

**Scalability:**
- **Separation of Concerns**: As streams grow to contain thousands or millions of events, the metadata collection remains small and performant.
- **Query Optimization**: Queries that only need stream metadata (e.g., checking if a stream exists, getting the current version) don't need to access the events collection at all.

This design pattern is common in event sourcing implementations and provides significant performance benefits as the event store scales.

