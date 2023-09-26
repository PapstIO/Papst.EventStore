using Newtonsoft.Json;

namespace Papst.EventStore.AzureCosmos.Database;

/// <summary>
/// Index Entity of an EventStream
/// </summary>
public class EventStreamIndexEntity
{
  [JsonProperty("id")]
  public string Id { get; } = CosmosEventStore.IndexDocumentName;
  
  /// <summary>
  /// The StreamId
  /// </summary>
  public Guid StreamId { get; init; }
  
  /// <summary>
  /// The time when the stream has been created
  /// </summary>
  public DateTimeOffset Created { get; init; }
  
  /// <summary>
  /// The Current version of the stream
  /// </summary>
  public ulong Version { get; set; }
  
  /// <summary>
  /// The Next Document Version
  /// </summary>
  public ulong NextVersion { get; set; }
  
  /// <summary>
  /// The time of the latest event
  /// </summary>
  public DateTimeOffset Updated { get; set; }
  
  /// <summary>
  /// The Target Type Name
  /// </summary>
  public string TargetType { get; init; } = string.Empty;
  
  /// <summary>
  /// The latest Snapshot version
  /// </summary>
  public ulong? LatestSnapshotVersion { get; init; }

  /// <summary>
  /// Cosmos Db ETag
  /// </summary>
  [JsonProperty("_etag")]
  public string ETag { get; set; } = string.Empty;
}
