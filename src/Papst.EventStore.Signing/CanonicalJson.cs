using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Papst.EventStore.Signing;

/// <summary>
/// Produces a deterministic byte representation of a JSON payload so a signature
/// computed on append verifies identically after the event has round-tripped
/// through any store's native serializer (Cosmos JObject, EF JSON string, Mongo
/// BSON, FileSystem JSON). Object property names are sorted ordinally; array order
/// is preserved.
/// </summary>
internal static class CanonicalJson
{
  public static byte[] ToCanonicalUtf8(JToken token)
    => Encoding.UTF8.GetBytes(Canonicalize(token).ToString(Formatting.None));

  private static JToken Canonicalize(JToken token) => token switch
  {
    JObject o => new JObject(
      o.Properties()
        .OrderBy(p => p.Name, StringComparer.Ordinal)
        .Select(p => new JProperty(p.Name, Canonicalize(p.Value)))),
    JArray a => new JArray(a.Select(Canonicalize)),
    _ => token.DeepClone(),
  };
}
