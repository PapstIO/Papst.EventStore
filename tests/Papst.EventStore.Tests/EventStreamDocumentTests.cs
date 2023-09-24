using AutoFixture.Xunit2;
using FluentAssertions;
using Newtonsoft.Json;
using Papst.EventStore.Abstractions.EventAggregation;
using System;
using Xunit;

namespace Papst.EventStore.Abstractions.Tests;

public class EventStreamDocumentTests
{
  [Theory, AutoData]
  public void TestEventStreamDocumentInitialization(
      Guid id,
      Guid streamid,
      EventStreamDocumentType docType,
      ulong version,
      DateTimeOffset time,
      string name,
      Type t)
  {
    var doc = new EventStreamDocument()
    {
      Id = id,
      StreamId = streamid,
      DocumentType = docType,
      Version = version,
      Time = time,
      Name = name,
      Data = null,
      DataType = TypeUtils.NameOfType(t),
      MetaData = null
    };

    doc.Should().NotBeNull();
    doc.Id.Should().NotBeEmpty().And.Be(id);
    doc.StreamId.Should().NotBeEmpty().And.Be(streamid);
    doc.DocumentType.Should().Be(docType);
    doc.Version.Should().Be(version);
    doc.Time.Should().Be(time);
    doc.Name.Should().Be(name);
    doc.DataType.Should().Be(TypeUtils.NameOfType(t));
    doc.Data.Should().BeNull();
    doc.MetaData.Should().BeNull();
  }

  [Theory, AutoData]
  public void TestEventStreamDocumentSerialization(EventStreamDocument doc)
  {
    doc.Should().NotBeNull();

    string serialized = JsonConvert.SerializeObject(doc);

    serialized.Should().NotBeNull().And.NotBeEmpty().And.StartWith("{");
  }

  [Theory, AutoData]
  public void TestEventStreamDocumentDeserialization(EventStreamDocument doc)
  {
    doc.Should().NotBeNull();

    EventStreamDocument deserialized = JsonConvert.DeserializeObject<EventStreamDocument>(JsonConvert.SerializeObject(doc));

    deserialized
        .Should()
        .NotBeNull();
    //deserialized.Should().BeSam.IsSameOrEqualTo(doc);
  }
}
