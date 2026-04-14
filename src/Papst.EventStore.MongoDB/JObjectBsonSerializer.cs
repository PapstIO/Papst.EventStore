using System.Text.Json.Nodes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Papst.EventStore.MongoDB;

/// <summary>
/// Custom BSON serializer for System.Text.Json JsonNode
/// </summary>
internal class JsonNodeBsonSerializer : SerializerBase<JsonNode>
{
  public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonNode value)
  {
    if (value == null)
    {
      context.Writer.WriteNull();
      return;
    }

    var bsonDocument = BsonDocument.Parse(value.ToJsonString());
    BsonDocumentSerializer.Instance.Serialize(context, bsonDocument);
  }

  public override JsonNode Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
  {
    var bsonDocument = BsonDocumentSerializer.Instance.Deserialize(context, args);
    if (bsonDocument == null)
    {
      return new JsonObject();
    }

    return JsonNode.Parse(bsonDocument.ToJson()) ?? new JsonObject();
  }
}
