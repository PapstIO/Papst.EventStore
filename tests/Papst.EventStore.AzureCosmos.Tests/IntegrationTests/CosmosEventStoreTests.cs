using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Papst.EventStore.AzureCosmos.Database;
using Xunit;

namespace Papst.EventStore.AzureCosmos.Tests.IntegrationTests;

public class CosmosEventStoreTests : IClassFixture<CosmosDbIntegrationTestFixture>
{
  private readonly IServiceProvider _serviceProvider;
  private readonly IEventStore _eventStore;

  public CosmosEventStoreTests(CosmosDbIntegrationTestFixture fixture)
  {
    _serviceProvider = fixture.BuildServiceProvider();
    _eventStore = _serviceProvider.GetRequiredService<IEventStore>();
  }

  [Theory, AutoData]
  public async Task CreateAsync_ShouldCreateIndexDocument(Guid streamId)
  {
    // arrange
    CosmosClient client = _serviceProvider.GetRequiredService<CosmosClient>();
    
    // act
    await _eventStore.CreateAsync(streamId, "", CancellationToken.None);
    
    // assert
    var container = client.GetContainer(CosmosDbIntegrationTestFixture.CosmosDatabaseName,
      CosmosDbIntegrationTestFixture.CosmosContainerId);
    var iterator = container.GetItemLinqQueryable<EventStreamIndexEntity>().ToFeedIterator();
    var batch = await iterator.ReadNextAsync();
    batch.Count.Should().Be(1);
    batch.Resource.First().StreamId.Should().Be(streamId);
    
  }
}
