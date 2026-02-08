using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.Exceptions;
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
    stream.StreamId.Should().Be(streamId);
    stream.Version.Should().Be(0);
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
    stream.StreamId.Should().Be(streamId);
    stream.Version.Should().Be(0);
    stream.MetaData.TenantId.Should().Be(tenantId);
    stream.MetaData.UserId.Should().Be(userId);
    stream.MetaData.UserName.Should().Be(username);
    stream.MetaData.Comment.Should().Be(comment);
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
}
