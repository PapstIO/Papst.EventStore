using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json.Linq;

namespace Papst.EventStore.MongoDB;

/// <summary>
/// Custom BSON serializer for Newtonsoft.Json JObject
/// </summary>
internal class JObjectSerializer : SerializerBase<JObject>
{
  public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JObject value)
  {
    if (value == null)
    {
      context.Writer.WriteNull();
      return;
    }

    var bsonDocument = BsonDocument.Parse(value.ToString());
    BsonDocumentSerializer.Instance.Serialize(context, bsonDocument);
  }

  public override JObject Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
  {
    var bsonDocument = BsonDocumentSerializer.Instance.Deserialize(context, args);
    if (bsonDocument == null)
    {
      return new JObject();
    }

    return JObject.Parse(bsonDocument.ToJson());
  }
}
