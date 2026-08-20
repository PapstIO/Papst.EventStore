using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit3;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.Exceptions;
using Shouldly;
using Xunit;

namespace Papst.EventStore.MongoDB.Tests.IntegrationTests;

public class MongoDBEventStoreTests : IClassFixture<MongoDBIntegrationTestFixture>
{
  private readonly MongoDBIntegrationTestFixture _fixture;

  public MongoDBEventStoreTests(MongoDBIntegrationTestFixture fixture) => _fixture = fixture;

  [Theory, AutoData]
  public async Task CreateAsync_ShouldCreateStream(Guid streamId)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();

    // act
    await eventStore.CreateAsync(streamId, "TestType", CancellationToken.None);

    // assert
    var stream = await eventStore.GetAsync(streamId, CancellationToken.None);
    stream.StreamId.ShouldBe(streamId);
    stream.Version.ShouldBe(0UL);
  }

  [Theory, AutoData]
  public async Task CreateAsync_WithMetadata_ShouldCreateStreamWithMetadata(Guid streamId, string tenantId, string userId, string username, string comment)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();

    // act
    await eventStore.CreateAsync(streamId, "TestType", tenantId, userId, username, comment, null, CancellationToken.None);

    // assert
    var stream = await eventStore.GetAsync(streamId, CancellationToken.None);
    stream.StreamId.ShouldBe(streamId);
    stream.Version.ShouldBe(0UL);
    stream.MetaData.TenantId.ShouldBe(tenantId);
    stream.MetaData.UserId.ShouldBe(userId);
    stream.MetaData.UserName.ShouldBe(username);
    stream.MetaData.Comment.ShouldBe(comment);
  }

  [Theory, AutoData]
  public async Task CreateAsync_WhenStreamExists_ShouldThrowException(Guid streamId)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    await eventStore.CreateAsync(streamId, "TestType", CancellationToken.None);

    // act & assert
    await Assert.ThrowsAsync<EventStreamAlreadyExistsException>(
      async () => await eventStore.CreateAsync(streamId, "TestType", CancellationToken.None)
    );
  }

  [Theory, AutoData]
  public async Task GetAsync_WhenStreamDoesNotExist_ShouldThrowException(Guid streamId)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();

    // act & assert
    await Assert.ThrowsAsync<EventStreamNotFoundException>(
      async () => await eventStore.GetAsync(streamId, CancellationToken.None)
    );
  }

  [Theory, AutoData]
  public async Task DeleteAsync_ShouldRemoveStream(Guid streamId, Guid eventId, string eventType)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    await eventStore.CreateAsync(streamId, "TestType", CancellationToken.None);
    var lowLevelStream = await eventStore.GetLowLevelAsync(streamId, CancellationToken.None);
    await lowLevelStream.AppendAsync(eventId, eventType, Newtonsoft.Json.Linq.JObject.FromObject(new { Value = "x" }), cancellationToken: CancellationToken.None);

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
    var serviceProvider = _fixture.BuildServiceProvider();
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
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    await eventStore.CreateAsync(streamId, "TestType", CancellationToken.None);
    var lowLevelStream = await eventStore.GetLowLevelAsync(streamId, CancellationToken.None);
    for (var i = 0; i < 3; i++)
    {
      await lowLevelStream.AppendAsync(Guid.NewGuid(), eventType, Newtonsoft.Json.Linq.JObject.FromObject(new { Index = i }), cancellationToken: CancellationToken.None);
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
