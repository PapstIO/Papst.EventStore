using AutoFixture.Xunit2;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Papst.EventStore;
using Papst.EventStore.Exceptions;
using Shouldly;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Papst.EventStore.EntityFrameworkCore.Tests.IntegrationTests;

public class EntityFrameworkCoreEventStoreTests
{
  private static EntityFrameworkCoreTestFixture CreateFixture() => new();

  [Theory, AutoData]
  public async Task DeleteAsync_ShouldRemoveStream(Guid streamId, Guid eventId, string eventType)
  {
    // arrange
    var serviceProvider = CreateFixture().BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    await eventStore.CreateAsync(streamId, "TestType", CancellationToken.None);
    var lowLevelStream = await eventStore.GetLowLevelAsync(streamId, CancellationToken.None);
    await lowLevelStream.AppendAsync(eventId, eventType, JObject.FromObject(new { Value = "x" }), cancellationToken: CancellationToken.None);

    // act
    await eventStore.DeleteAsync(streamId, CancellationToken.None);

    // assert
    await Assert.ThrowsAsync<EventStreamNotFoundException>(
      async () => await eventStore.GetAsync(streamId, CancellationToken.None)
    );
  }

  [Theory, AutoData]
  public async Task DeleteAsync_WhenStreamDoesNotExist_ShouldThrow(Guid streamId)
  {
    // arrange
    var serviceProvider = CreateFixture().BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();

    // act & assert
    await Assert.ThrowsAsync<EventStreamNotFoundException>(
      async () => await eventStore.DeleteAsync(streamId, CancellationToken.None)
    );
  }

  [Theory, AutoData]
  public async Task DeleteAsync_ShouldRemoveAllDocuments_AndAllowRecreation(Guid streamId, string eventType)
  {
    // arrange
    var fixture = CreateFixture();
    var serviceProvider = fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    await eventStore.CreateAsync(streamId, "TestType", CancellationToken.None);
    var lowLevelStream = await eventStore.GetLowLevelAsync(streamId, CancellationToken.None);
    for (var i = 0; i < 3; i++)
    {
      await lowLevelStream.AppendAsync(Guid.NewGuid(), eventType, JObject.FromObject(new { Index = i }), cancellationToken: CancellationToken.None);
    }

    // act
    await eventStore.DeleteAsync(streamId, CancellationToken.None);
    var recreated = await eventStore.CreateAsync(streamId, "TestType", CancellationToken.None);

    // assert - no leftover documents from the deleted stream
    var events = await recreated.ListAsync(0, CancellationToken.None).ToListAsync(CancellationToken.None);
    events.ShouldBeEmpty();
    recreated.Version.ShouldBe(0UL);
  }
}
