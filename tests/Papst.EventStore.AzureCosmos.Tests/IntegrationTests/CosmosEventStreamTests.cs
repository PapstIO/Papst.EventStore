using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.Aggregation;
using Papst.EventStore.AzureCosmos.Tests.IntegrationTests.Models;
using Xunit;

namespace Papst.EventStore.AzureCosmos.Tests.IntegrationTests;
public class CosmosEventStreamTests : IClassFixture<CosmosDbIntegrationTestFixture>
{
  private readonly CosmosDbIntegrationTestFixture _fixture;

  public CosmosEventStreamTests(CosmosDbIntegrationTestFixture fixture) => _fixture = fixture;

  private async Task<IEventStream> CreateStreamAsync(IEventStore store, Guid streamId, params TestAppendedEvent[] events)
  {
    var stream = await store.CreateAsync(streamId, nameof(TestEntity));

    await stream.AppendAsync(Guid.NewGuid(), new TestCreatedEvent());

    foreach (TestAppendedEvent evt in events)
    {
      await stream.AppendAsync(Guid.NewGuid(), evt);
    }

    return stream;
  }

  [Theory, AutoData]
  public async Task AppendAsync_ShouldAppendEvent(Guid streamId, Guid documentId)
  {
    // arrange
    var services = _fixture.BuildServiceProvider();
    var store = services.GetRequiredService<IEventStore>();
    var stream = await CreateStreamAsync(store, streamId);

    // act
    await stream.AppendAsync(documentId, new TestAppendedEvent());

    // assert
    stream.Version.Should().Be(1);
    var events = await stream.ListAsync().ToListAsync();
    events.Should().Contain(d => d.Id == documentId);
  }
  
  [Theory, AutoData]
  public async Task AppendTransactionAsync_ShouldAppendEvents(Guid streamId)
  {  
    // arrange
    var services = _fixture.BuildServiceProvider();
    var store = services.GetRequiredService<IEventStore>();
    var stream = await CreateStreamAsync(store, streamId);
    
    // act
    var batch = await stream.CreateTransactionalBatchAsync();
    batch.Add(Guid.NewGuid(), new TestAppendedEvent());
    batch.Add(Guid.NewGuid(), new TestAppendedEvent());
    batch.Add(Guid.NewGuid(), new TestAppendedEvent());
    batch.Add(Guid.NewGuid(), new TestAppendedEvent());
    await batch.CommitAsync(); 

    // assert
    var events = await stream.ListAsync(0).ToListAsync();
    events.Should().HaveCount(5);
    
  }
  
  [Theory, AutoData]
  public async Task AppenSnapshotAsync_ShouldAppendSnapshot(Guid streamId, Guid documentId)
  {
    // arrange
    var services = _fixture.BuildServiceProvider();
    var store = services.GetRequiredService<IEventStore>();
    var stream = await CreateStreamAsync(store, streamId);
    var aggregator = services.GetRequiredService<IEventStreamAggregator<TestEntity>>();
    var entity = await aggregator.AggregateAsync(stream, CancellationToken.None);
    
    // act
    Func<Task> act = async () => await stream.AppendSnapshotAsync(documentId, entity, null, CancellationToken.None);
    
    // assert
    await act.Should().NotThrowAsync();
    entity.Should().NotBeNull();
    stream.LatestSnapshotVersion.Should().HaveValue().And.Be(entity!.Version);
  }
  
  [Theory, AutoData]
  public async Task ListAsync_ShouldListEvents(Guid streamId)
  {  
    // arrange
    var services = _fixture.BuildServiceProvider();
    var store = services.GetRequiredService<IEventStore>();
    var stream = await CreateStreamAsync(store, streamId, new TestAppendedEvent(), new TestAppendedEvent(), new TestAppendedEvent());

    // act
    var list = stream.ListAsync(0);
    var events = await list.ToListAsync();

    // assert
    events.Count.Should().Be(4, "3 TestAppendedEvent + 1 TestCreatedEvent");
  }
}
