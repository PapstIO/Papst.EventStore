namespace Papst.EventStore.MongoDB;

/// <summary>
/// Configuration options for MongoDB EventStore
/// </summary>
public class MongoDBEventStoreOptions
{
  /// <summary>
  /// MongoDB connection string
  /// </summary>
  public string ConnectionString { get; set; } = "mongodb://localhost:27017";

  /// <summary>
  /// Database name for the event store
  /// </summary>
  public string DatabaseName { get; set; } = "EventStore";

  /// <summary>
  /// Collection name for event stream documents
  /// </summary>
  public string CollectionName { get; set; } = "EventStreams";

  /// <summary>
  /// Collection name for stream metadata
  /// </summary>
  public string StreamMetadataCollectionName { get; set; } = "StreamMetadata";
}
