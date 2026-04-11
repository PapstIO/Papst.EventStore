using AutoFixture.Xunit2;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventsStore.InMemory.Tests.IntegrationTests.Events;
using Papst.EventStore;
using Shouldly;

namespace Papst.EventsStore.InMemory.Tests.IntegrationTests;

public class InMemoryEventStreamTests: IClassFixture<InMemoryTestFixture>
{
  private readonly InMemoryTestFixture _fixture;
  public InMemoryEventStreamTests(InMemoryTestFixture fixture) => _fixture = fixture;

  [Theory, AutoData]
  public async Task ListAsync_ShouldReturnEmpty(Guid streamId)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    var stream = await eventStore.CreateAsync(streamId, "", CancellationToken.None);
    
    // act
    
    var events = await stream.ListAsync(0, CancellationToken.None).ToListAsync(CancellationToken.None);
    
    // assert
    events.ShouldBeEmpty();
  }
  
  [Theory, AutoData]
  public async Task ListAsync_ShouldReturnEntries(List<TestEvent> events, Guid streamId)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    var stream = await eventStore.CreateAsync(streamId, "", CancellationToken.None);
    foreach (var @event in events)
    {
      await stream.AppendAsync(Guid.NewGuid(), @event, cancellationToken: CancellationToken.None);
    }
    
    // act
    var result = await stream.ListAsync(0, CancellationToken.None).ToListAsync(CancellationToken.None);
    
    // assert
    result.Count.ShouldBe(events.Count);
    result.Select(e => e.Data.ToObject<TestEvent>())
      .Where(e => e is not null)
      .Select(e => e!)
      .ToList()
      .ShouldBe(events);
  }
  
}
