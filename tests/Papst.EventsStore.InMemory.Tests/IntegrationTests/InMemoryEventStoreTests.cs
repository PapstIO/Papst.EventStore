using AutoFixture.Xunit2;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore;
using Papst.EventStore.Exceptions;
using Shouldly;

namespace Papst.EventsStore.InMemory.Tests.IntegrationTests;

public class InMemoryEventStoreTests : IClassFixture<InMemoryTestFixture>
{
  private readonly InMemoryTestFixture _fixture;
  public InMemoryEventStoreTests(InMemoryTestFixture fixture) => _fixture = fixture;
  
  [Theory, AutoData]
  public async Task CreateAsync_ShouldCreateStream(Guid streamId)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    
    // act
    await eventStore.CreateAsync(streamId, "", CancellationToken.None);
    
    // assert
    var stream = await eventStore.GetAsync(streamId, CancellationToken.None);
    stream.StreamId.ShouldBe(streamId);
    stream.Version.ShouldBe(0UL);
  }

  [Fact]
  public async Task CreateAsync_ShouldCreateAllStreams_WhenCalledConcurrently()
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    var streamIds = Enumerable.Range(0, 1_000).Select(_ => Guid.NewGuid()).ToList();

    // act
    await Task.WhenAll(streamIds.Select(id =>
      Task.Run(() => eventStore.CreateAsync(id, "", CancellationToken.None))));

    // assert
    foreach (var id in streamIds)
    {
      (await eventStore.GetAsync(id, CancellationToken.None)).StreamId.ShouldBe(id);
    }
  }

  [Theory, AutoData]
  public async Task CreateAsync_ShouldThrowAlreadyExists_WhenSameStreamCreatedConcurrently(Guid streamId)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();

    // act
    var results = await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(async () =>
    {
      try
      {
        await eventStore.CreateAsync(streamId, "", CancellationToken.None);
        return null;
      }
      catch (Exception ex)
      {
        return ex;
      }
    })));

    // assert
    results.Count(r => r is null).ShouldBe(1);
    results.Where(r => r is not null).ShouldAllBe(r => r is EventStreamAlreadyExistsException);
  }
}
