using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Papst.EventStore.Documents;

namespace Papst.EventStore.AzureCosmos.Database;

public class EventStreamDocumentEntity
{
  [JsonProperty("id")]
  public string Id { get; set; } = null!;

  /// <summary>
  /// The Unique Event Id
  /// </summary>
  public Guid DocumentId { get; set; }

  /// <summary>
  /// The Event Stream Id
  /// </summary>
  [JsonProperty(nameof(StreamId))]
  public Guid StreamId { get; set; }

  /// <summary>
  /// Type of the Document
  /// </summary>
  [JsonConverter(typeof(StringEnumConverter))]
  public EventStreamDocumentType DocumentType { get; set; }

  /// <summary>
  /// Version of the Document after applying this Event
  /// </summary>
  public ulong Version { get; set; }

  /// <summary>
  /// The Time of the Event
  /// </summary>
  public DateTimeOffset Time { get; set; }

  /// <summary>
  /// Name of the Event
  /// </summary>
  public string Name { get; set; } = null!;

  /// <summary>
  /// Data of the Event as JSON Object
  /// </summary>
  public JObject Data { get; set; } = null!;

  /// <summary>
  /// Type of the Data
  /// </summary>
  public string DataType { get; set; } = null!;

  /// <summary>
  /// The type on which the Event will be applied
  /// </summary>
  public string TargetType { get; set; } = null!;

  /// <summary>
  /// Metadata for the Event
  /// </summary>
  public EventStreamMetaData MetaData { get; set; } = null!;
}
