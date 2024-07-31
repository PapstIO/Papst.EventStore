using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Papst.EventStore.Documents;

/// <summary>
/// Representation of an Event Stream Entry
/// </summary>
public record EventStreamDocument
{
  /// <summary>
  /// The Unique Event Id
  /// </summary>
  public Guid Id { get; init; }

  /// <summary>
  /// The Event Stream Id
  /// </summary>
  public Guid StreamId { get; init; }

  /// <summary>
  /// Type of the Document
  /// </summary>
  [JsonConverter(typeof(StringEnumConverter))]
  public EventStreamDocumentType DocumentType { get; init; }

  /// <summary>
  /// Version of the Document after applying this Event
  /// </summary>
  public ulong Version { get; init; }

  /// <summary>
  /// The Time of the Event
  /// </summary>
  public DateTimeOffset Time { get; init; }

  /// <summary>
  /// Name of the Event
  /// </summary>
  public string Name { get; init; } = string.Empty;

  /// <summary>
  /// Data of the Event as JSON Object
  /// </summary>
  public JObject Data { get; init; } = JObject.FromObject(new object());

  /// <summary>
  /// Type of the Data
  /// </summary>
  public string DataType { get; init; } = string.Empty;

  /// <summary>
  /// The type on which the Event will be applied
  /// </summary>
  public string TargetType { get; init; } = string.Empty;

  /// <summary>
  /// Metadata for the Event
  /// </summary>
  public EventStreamMetaData MetaData { get; init; } = new();

  /// <summary>
  /// Create a new <see cref="EventStreamDocument" /> using an Event
  /// </summary>
  /// <typeparam name="TEvent"></typeparam>
  /// <param name="streamId"></param>
  /// <param name="id"></param>
  /// <param name="version"></param>
  /// <param name="name"></param>
  /// <param name="data"></param>
  /// <param name="dataType"></param>
  /// <param name="targetType"></param>
  /// <param name="metaData"></param>
  /// <param name="documentType"></param>
  /// <returns></returns>
  public static EventStreamDocument Create<TEvent>(Guid streamId, Guid id, ulong version, string name, TEvent data, string dataType, string targetType, EventStreamMetaData? metaData = null, EventStreamDocumentType documentType = EventStreamDocumentType.Event)
    where TEvent : notnull
    => new EventStreamDocument
    {
      Id = id,
      StreamId = streamId,
      DocumentType = documentType,
      Version = version,
      Name = name,
      Time = DateTimeOffset.Now,
      Data = JObject.FromObject(data),
      DataType = dataType,
      TargetType = targetType,
    };
}
