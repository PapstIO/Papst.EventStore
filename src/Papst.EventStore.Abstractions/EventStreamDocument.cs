using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;

namespace Papst.EventStore.Abstractions;

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
  public string Name { get; init; } = null!;

  /// <summary>
  /// Data of the Event as JSON Object
  /// </summary>
  public JObject Data { get; init; } = null!;

  /// <summary>
  /// Type of the Data
  /// </summary>
  public Type DataType { get; init; } = null!;

  /// <summary>
  /// The type on which the Event will be applied
  /// </summary>
  public Type TargetType { get; init; } = null!;

  /// <summary>
  /// Metadata for the Event
  /// </summary>
  public EventStreamMetaData MetaData { get; init; } = null!;
}
