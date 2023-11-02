using AutoFixture.Xunit2;
using FluentAssertions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Papst.EventStore.Documents;
using Xunit;

namespace Papst.EventStore.Abstractions.Tests;

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

    meta.Should().NotBeNull();
    meta.UserId.Should().Be(id);
    meta.UserName.Should().Be(name);
    meta.TenantId.Should().Be(tenant);
    meta.Comment.Should().Be(comment);
    meta.Additional.Should().BeEquivalentTo(add);
  }

  [Theory, AutoData]
  public void TestEventMetaDataSerialization(EventStreamMetaData doc)
  {
    doc.Should().NotBeNull();

    string serialized = JsonConvert.SerializeObject(doc);

    serialized.Should().NotBeNull().And.NotBeEmpty().And.StartWith("{");
  }

  [Theory, AutoData]
  public void TestEventMetaDataDeserialization(EventStreamMetaData doc)
  {
    doc.Should().NotBeNull();

    JsonConvert.DeserializeObject<EventStreamMetaData>(JsonConvert.SerializeObject(doc))
        .Should()
        .NotBeNull()
        .And.BeEquivalentTo(doc);
  }
}
