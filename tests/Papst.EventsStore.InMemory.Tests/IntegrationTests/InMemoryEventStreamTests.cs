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

  [Theory, AutoData]
  public async Task AppendAsync_ShouldAssignSequentialEventVersions(List<TestEvent> events, Guid streamId)
  {
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    var stream = await eventStore.CreateAsync(streamId, "", CancellationToken.None);
    foreach (var @event in events)
    {
      await stream.AppendAsync(Guid.NewGuid(), @event, cancellationToken: CancellationToken.None);
    }

    var result = await stream.ListAsync(0, CancellationToken.None).ToListAsync(CancellationToken.None);

    result.Select(e => e.Version).ShouldBe(Enumerable.Range(1, events.Count).Select(i => (ulong)i));
  }

  [Theory, AutoData]
  public async Task AppendAsync_ShouldAdvanceStreamVersion(List<TestEvent> events, Guid streamId)
  {
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    var stream = await eventStore.CreateAsync(streamId, "", CancellationToken.None);
    foreach (var @event in events)
    {
      await stream.AppendAsync(Guid.NewGuid(), @event, cancellationToken: CancellationToken.None);
    }

    stream.Version.ShouldBe((ulong)events.Count);
  }

  [Theory, AutoData]
  public async Task AppendSnapshotAsync_ShouldAdvanceStreamVersion(TestEvent firstEvent, TestEvent snapshot, Guid streamId)
  {
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    var stream = await eventStore.CreateAsync(streamId, "", CancellationToken.None);
    await stream.AppendAsync(Guid.NewGuid(), firstEvent, cancellationToken: CancellationToken.None);
    await stream.AppendSnapshotAsync(Guid.NewGuid(), snapshot, cancellationToken: CancellationToken.None);

    var documents = await stream.ListAsync(0, CancellationToken.None).ToListAsync(CancellationToken.None);
    documents.Select(d => d.Version).ShouldBe([1UL, 2UL]);
    stream.Version.ShouldBe(2UL);
  }

  [Theory, AutoData]
  public async Task ListAsync_FromVersionGreaterThanOne_ShouldReturnEventsAppendedAfterFirst(TestEvent first, TestEvent second, Guid streamId)
  {
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    var stream = await eventStore.CreateAsync(streamId, "", CancellationToken.None);
    await stream.AppendAsync(Guid.NewGuid(), first, cancellationToken: CancellationToken.None);
    await stream.AppendAsync(Guid.NewGuid(), second, cancellationToken: CancellationToken.None);

    var result = await stream.ListAsync(2, CancellationToken.None).ToListAsync(CancellationToken.None);

    result.Count.ShouldBe(1);
    result[0].Data.ToObject<TestEvent>().ShouldBe(second);
  }
}
