using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Papst.EventStore;
using Papst.EventStore.Documents;
using Papst.EventStore.Exceptions;
using Shouldly;
using Xunit;

namespace Papst.EventStore.MongoDB.Tests.IntegrationTests;

public class MongoDBLowLevelEventStreamTests : IClassFixture<MongoDBIntegrationTestFixture>
{
  private readonly MongoDBIntegrationTestFixture _fixture;

  public MongoDBLowLevelEventStreamTests(MongoDBIntegrationTestFixture fixture) => _fixture = fixture;

  [Theory, AutoData]
  public async Task GetLowLevelAsync_ShouldReturnLowLevelStream(Guid streamId)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    await eventStore.CreateAsync(streamId, "TestType", CancellationToken.None);

    // act
    var stream = await eventStore.GetLowLevelAsync(streamId, CancellationToken.None);

    // assert
    stream.ShouldNotBeNull();
    stream.ShouldBeAssignableTo<ILowLevelEventStream>();
  }

  [Theory, AutoData]
  public async Task GetLowLevelAsync_WhenStreamDoesNotExist_ShouldThrow(Guid streamId)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();

    // act & assert
    await Assert.ThrowsAsync<EventStreamNotFoundException>(
      async () => await eventStore.GetLowLevelAsync(streamId, CancellationToken.None)
    );
  }

  [Theory, AutoData]
  public async Task AppendAsync_LowLevel_ShouldAppendEventToStream(Guid streamId, Guid eventId, string eventType)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    await eventStore.CreateAsync(streamId, "TestType", CancellationToken.None);
    var lowLevelStream = await eventStore.GetLowLevelAsync(streamId, CancellationToken.None);

    var payload = JObject.FromObject(new { Value = "test-value", Number = 42 });

    // act
    await lowLevelStream.AppendAsync(eventId, eventType, payload, cancellationToken: CancellationToken.None);

    // assert
    var stream = await eventStore.GetAsync(streamId, CancellationToken.None);
    var events = await stream.ListAsync(0, CancellationToken.None).ToListAsync(CancellationToken.None);
    events.Count.ShouldBe(1);
    events[0].DataType.ShouldBe(eventType);
    events[0].Data["Value"]?.ToString().ShouldBe("test-value");
  }

  [Theory, AutoData]
  public async Task AppendAsync_LowLevel_WithMetaData_ShouldStoreMetaData(Guid streamId, Guid eventId, string eventType, string userId)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    await eventStore.CreateAsync(streamId, "TestType", CancellationToken.None);
    var lowLevelStream = await eventStore.GetLowLevelAsync(streamId, CancellationToken.None);

    var payload = JObject.FromObject(new { Value = "meta-test" });
    var metaData = new EventStreamMetaData { UserId = userId };

    // act
    await lowLevelStream.AppendAsync(eventId, eventType, payload, metaData, CancellationToken.None);

    // assert
    var stream = await eventStore.GetAsync(streamId, CancellationToken.None);
    var events = await stream.ListAsync(0, CancellationToken.None).ToListAsync(CancellationToken.None);
    events.Count.ShouldBe(1);
    events[0].MetaData?.UserId.ShouldBe(userId);
  }

  [Theory, AutoData]
  public async Task AppendAsync_LowLevel_MultipleTimes_ShouldIncrementVersion(Guid streamId, string eventType)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    await eventStore.CreateAsync(streamId, "TestType", CancellationToken.None);
    var lowLevelStream = await eventStore.GetLowLevelAsync(streamId, CancellationToken.None);

    // act
    for (var i = 0; i < 3; i++)
    {
      await lowLevelStream.AppendAsync(Guid.NewGuid(), eventType, JObject.FromObject(new { Index = i }), cancellationToken: CancellationToken.None);
    }

    // assert
    var stream = await eventStore.GetAsync(streamId, CancellationToken.None);
    var events = await stream.ListAsync(0, CancellationToken.None).ToListAsync(CancellationToken.None);
    events.Count.ShouldBe(3);
  }
}
