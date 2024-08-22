using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.AzureCosmos.Database;
using Papst.EventStore.Exceptions;
using Xunit;

namespace Papst.EventStore.AzureCosmos.Tests.IntegrationTests;

public class CosmosEventStoreTests : IClassFixture<CosmosDbIntegrationTestFixture>
{
  private readonly CosmosDbIntegrationTestFixture _fixture;

  public CosmosEventStoreTests(CosmosDbIntegrationTestFixture fixture) => _fixture = fixture;

  [Theory, AutoData]
  public async Task CreateAsync_ShouldCreateIndexDocument(Guid streamId)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    CosmosClient client = serviceProvider.GetRequiredService<CosmosClient>();

    // act
    await eventStore.CreateAsync(streamId, "", CancellationToken.None);

    // assert
    var container = client.GetContainer(CosmosDbIntegrationTestFixture.CosmosDatabaseName,
      CosmosDbIntegrationTestFixture.CosmosContainerId);
    var iterator = container.GetItemLinqQueryable<EventStreamIndexEntity>().ToFeedIterator();
    var batch = await iterator.ReadNextAsync();
    batch.Count.Should().Be(1);
    batch.Resource.First().StreamId.Should().Be(streamId);
  }

  [Theory, AutoData]
  public async Task GetAsync_ShouldThrow_WhenNotFound(Guid streamId)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();

    // act
    Func<Task> act = () => eventStore.GetAsync(streamId, CancellationToken.None);

    // assert
    await act.Should().ThrowAsync<EventStreamNotFoundException>();
  }

  [Theory, AutoData]
  public async Task GetAsync_ShouldReturnStream(EventStreamIndexEntity index)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    CosmosClient client = serviceProvider.GetRequiredService<CosmosClient>();
    var container = client.GetContainer(CosmosDbIntegrationTestFixture.CosmosDatabaseName, CosmosDbIntegrationTestFixture.CosmosContainerId);
    await container.UpsertItemAsync(index);

    // act
    var stream = await eventStore.GetAsync(index.StreamId, CancellationToken.None);

    // assert
    stream.Should().NotBeNull();
    stream.StreamId.Should().Be(index.StreamId);
  }
}
