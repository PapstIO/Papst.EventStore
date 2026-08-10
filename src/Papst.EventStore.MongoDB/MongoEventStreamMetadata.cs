using System;
using MongoDB.Bson.Serialization.Attributes;
using Papst.EventStore.Documents;

namespace Papst.EventStore.MongoDB;

/// <summary>
/// MongoDB document for storing event stream metadata
/// </summary>
internal class MongoEventStreamMetadata
{
  [BsonId]
  [BsonRepresentation(global::MongoDB.Bson.BsonType.String)]
  public Guid StreamId { get; set; }
  
  public ulong Version { get; set; }

  /// <summary>
  /// Version to assign to the next appended event. Newly created streams start at 0 so
  /// the first event is version 0, matching the other stores (Azure Cosmos, EF Core,
  /// FileSystem).
  /// </summary>
  public ulong NextVersion { get; set; }

  public DateTimeOffset Created { get; set; }
  public string TargetTypeName { get; set; } = string.Empty;
  public EventStreamMetaData MetaData { get; set; } = new();
  public ulong? LatestSnapshotVersion { get; set; }
}
