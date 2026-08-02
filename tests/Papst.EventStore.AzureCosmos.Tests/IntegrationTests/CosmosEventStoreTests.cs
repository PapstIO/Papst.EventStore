using AutoFixture.Xunit2;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.AzureCosmos.Database;
using Papst.EventStore.Exceptions;
using Shouldly;
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
    batch.Count.ShouldBe(1);
    batch.Resource.First().StreamId.ShouldBe(streamId);
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
    await Should.ThrowAsync<EventStreamNotFoundException>(act);
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
    stream.ShouldNotBeNull();
    stream.StreamId.ShouldBe(index.StreamId);
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
    await Should.ThrowAsync<EventStreamNotFoundException>(
      () => eventStore.GetAsync(streamId, CancellationToken.None));
  }

  [Theory, AutoData]
  public async Task DeleteAsync_WhenStreamDoesNotExist_ShouldThrow(Guid streamId)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();

    // act & assert
    await Should.ThrowAsync<EventStreamNotFoundException>(
      () => eventStore.DeleteAsync(streamId, CancellationToken.None));
  }

  [Theory, AutoData]
  public async Task DeleteAsync_ShouldRemoveAllDocumentsInPartition(Guid streamId, string eventType)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    CosmosClient client = serviceProvider.GetRequiredService<CosmosClient>();
    var container = client.GetContainer(CosmosDbIntegrationTestFixture.CosmosDatabaseName, CosmosDbIntegrationTestFixture.CosmosContainerId);
    await eventStore.CreateAsync(streamId, "TestType", CancellationToken.None);
    var lowLevelStream = await eventStore.GetLowLevelAsync(streamId, CancellationToken.None);
    for (var i = 0; i < 3; i++)
    {
      await lowLevelStream.AppendAsync(Guid.NewGuid(), eventType, Newtonsoft.Json.Linq.JObject.FromObject(new { Index = i }), cancellationToken: CancellationToken.None);
    }

    // act
    await eventStore.DeleteAsync(streamId, CancellationToken.None);

    // assert - no documents remain for the stream's partition
    var iterator = container
      .GetItemLinqQueryable<EventStreamDocumentEntity>(requestOptions: new() { PartitionKey = new PartitionKey(streamId.ToString()) })
      .Where(d => d.StreamId == streamId)
      .ToFeedIterator();
    var batch = await iterator.ReadNextAsync();
    batch.Count.ShouldBe(0);
  }
}
