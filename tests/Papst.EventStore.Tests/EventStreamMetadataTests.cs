using System.Collections.Generic;
using System.Text.Json;
using AutoFixture.Xunit2;
using Papst.EventStore.Documents;
using Shouldly;
using Xunit;

namespace Papst.EventStore.Tests;

public class EventStreamMetadataTests
{
  [Theory, AutoData]
  public void TestEventStreamMetaDataInitialization(
      string id,
      string name,
      string tenant,
      string comment,
      Dictionary<string, string> add
  )
  {
    var meta = new EventStreamMetaData
    {
      UserId = id,
      UserName = name,
      TenantId = tenant,
      Comment = comment,
      Additional = add
    };

    meta.ShouldNotBeNull();
    meta.UserId.ShouldBe(id);
    meta.UserName.ShouldBe(name);
    meta.TenantId.ShouldBe(tenant);
    meta.Comment.ShouldBe(comment);
    meta.Additional.ShouldNotBeNull();
    meta.Additional.Count.ShouldBe(add.Count);
    foreach (var entry in add)
    {
      meta.Additional[entry.Key].ShouldBe(entry.Value);
    }
  }

  [Theory, AutoData]
  public void TestEventMetaDataSerialization(EventStreamMetaData doc)
  {
    doc.ShouldNotBeNull();

    string serialized = JsonSerializer.Serialize(doc);

    serialized.ShouldNotBeNullOrEmpty();
    serialized.ShouldStartWith("{");
  }

  [Theory, AutoData]
  public void TestEventMetaDataDeserialization(EventStreamMetaData doc)
  {
    doc.ShouldNotBeNull();

    var deserialized = JsonSerializer.Deserialize<EventStreamMetaData>(JsonSerializer.Serialize(doc));

    deserialized.ShouldNotBeNull();
    deserialized.UserId.ShouldBe(doc.UserId);
    deserialized.UserName.ShouldBe(doc.UserName);
    deserialized.TenantId.ShouldBe(doc.TenantId);
    deserialized.Comment.ShouldBe(doc.Comment);
    deserialized.Additional.ShouldBe(doc.Additional);
  }
}
