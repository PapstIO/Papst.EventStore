using Newtonsoft.Json.Linq;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Papst.EventStore.FileSystem;

/// <summary>
/// Converts between System.Text.Json and Newtonsoft.Json.Linq.JObject
/// so that EventStreamDocument can be stored and retrieved by the FileSystem provider.
/// </summary>
internal sealed class JObjectJsonConverter : JsonConverter<JObject>
{
  public override JObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    using var doc = JsonDocument.ParseValue(ref reader);
    return JObject.Parse(doc.RootElement.GetRawText());
  }

  public override void Write(Utf8JsonWriter writer, JObject value, JsonSerializerOptions options)
  {
    writer.WriteRawValue(value.ToString(Newtonsoft.Json.Formatting.None));
  }
}
