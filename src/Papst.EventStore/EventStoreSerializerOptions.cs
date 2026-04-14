using System.Text.Json;

namespace Papst.EventStore;

/// <summary>
/// Options for configuring JSON serialization in the event store.
/// When configured with a source-generated JsonSerializerContext,
/// enables AOT-safe serialization.
/// </summary>
public class EventStoreSerializerOptions
{
  /// <summary>
  /// The JsonSerializerOptions to use for serializing/deserializing events.
  /// When null, default System.Text.Json behavior is used.
  /// </summary>
  public JsonSerializerOptions? JsonSerializerOptions { get; set; }
}
