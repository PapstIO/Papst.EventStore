using AutoFixture.Xunit3;
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
  public async Task AppendAsync_ShouldNumberFirstEventZero(TestEvent first, Guid streamId)
  {
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    var stream = await eventStore.CreateAsync(streamId, "", CancellationToken.None);

    await stream.AppendAsync(Guid.NewGuid(), first, cancellationToken: CancellationToken.None);

    var result = await stream.ListAsync(0, CancellationToken.None).ToListAsync(CancellationToken.None);
    result.Single().Version.ShouldBe(0UL);
    stream.Version.ShouldBe(0UL);
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

    result.Select(e => e.Version).ShouldBe(Enumerable.Range(0, events.Count).Select(i => (ulong)i));
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

    // 0-based versions: after N events the latest version is N - 1.
    stream.Version.ShouldBe((ulong)events.Count - 1);
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
    documents.Select(d => d.Version).ShouldBe([0UL, 1UL]);
    stream.Version.ShouldBe(1UL);
  }

  [Theory, AutoData]
  public async Task ListAsync_FromVersionGreaterThanZero_ShouldReturnEventsAppendedAfterFirst(TestEvent first, TestEvent second, Guid streamId)
  {
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    var stream = await eventStore.CreateAsync(streamId, "", CancellationToken.None);
    await stream.AppendAsync(Guid.NewGuid(), first, cancellationToken: CancellationToken.None);
    await stream.AppendAsync(Guid.NewGuid(), second, cancellationToken: CancellationToken.None);

    var result = await stream.ListAsync(1, CancellationToken.None).ToListAsync(CancellationToken.None);

    result.Count.ShouldBe(1);
    result[0].Data.ToObject<TestEvent>().ShouldBe(second);
  }

  [Theory, AutoData]
  public async Task AppendAsync_ShouldRetainAllEventsWithUniqueVersions_WhenAppendedConcurrently(TestEvent @event, Guid streamId)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    var stream = await eventStore.CreateAsync(streamId, "", CancellationToken.None);

    // act
    await Task.WhenAll(Enumerable.Range(0, 500).Select(_ =>
      Task.Run(() => stream.AppendAsync(Guid.NewGuid(), @event, cancellationToken: CancellationToken.None))));

    // assert
    stream.Version.ShouldBe(499UL);
    var documents = await stream.ListAsync(0, CancellationToken.None).ToListAsync(CancellationToken.None);
    documents.Count.ShouldBe(500);
    documents.Select(d => d.Version).ShouldBeUnique();
  }

  private async Task<IEventStream> CreateMixedStreamAsync(Guid streamId)
  {
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    var stream = await eventStore.CreateAsync(streamId, "", CancellationToken.None);
    await stream.AppendAsync(Guid.NewGuid(), new TestEvent(), cancellationToken: CancellationToken.None);
    await stream.AppendAsync(Guid.NewGuid(), new OtherTestEvent(), cancellationToken: CancellationToken.None);
    await stream.AppendAsync(Guid.NewGuid(), new TestEvent(), cancellationToken: CancellationToken.None);
    await stream.AppendAsync(Guid.NewGuid(), new OtherTestEvent(), cancellationToken: CancellationToken.None);
    return stream;
  }

  [Theory, AutoData]
  public async Task ListAsync_ShouldReturnOnlyEventsWithGivenNames(Guid streamId)
  {
    // arrange
    var stream = await CreateMixedStreamAsync(streamId);

    // act
    var result = await stream.ListAsync(0, stream.Version, [nameof(OtherTestEvent)], CancellationToken.None)
      .ToListAsync(CancellationToken.None);

    // assert
    result.Select(d => d.Version).ShouldBe([1ul, 3ul]);
    result.ShouldAllBe(d => d.Name == nameof(OtherTestEvent));
  }

  [Theory, AutoData]
  public async Task ListAsync_ShouldRespectVersionRange_WhenFilteringByName(Guid streamId)
  {
    // arrange
    var stream = await CreateMixedStreamAsync(streamId);

    // act
    var result = await stream.ListAsync(0, 2, [nameof(OtherTestEvent)], CancellationToken.None)
      .ToListAsync(CancellationToken.None);

    // assert
    result.Select(d => d.Version).ShouldBe([1ul]);
  }

  [Theory, AutoData]
  public async Task ListAsync_ShouldReturnEventsOfAllGivenNames(Guid streamId)
  {
    // arrange
    var stream = await CreateMixedStreamAsync(streamId);

    // act
    var result = await stream
      .ListAsync(0, stream.Version, [nameof(TestEvent), nameof(OtherTestEvent)], CancellationToken.None)
      .ToListAsync(CancellationToken.None);

    // assert
    result.Select(d => d.Version).ShouldBe([0ul, 1ul, 2ul, 3ul]);
  }

  [Theory, AutoData]
  public async Task ListAsync_ShouldReturnEmpty_WhenNoEventNamesAreGiven(Guid streamId)
  {
    // arrange
    var stream = await CreateMixedStreamAsync(streamId);

    // act
    var result = await stream.ListAsync(0, stream.Version, [], CancellationToken.None)
      .ToListAsync(CancellationToken.None);

    // assert
    result.ShouldBeEmpty();
  }

  [Theory, AutoData]
  public async Task ListDescendingAsync_ShouldReturnOnlyEventsWithGivenNames(Guid streamId)
  {
    // arrange
    var stream = await CreateMixedStreamAsync(streamId);

    // act
    var result = await stream.ListDescendingAsync(stream.Version, 0, [nameof(TestEvent)], CancellationToken.None)
      .ToListAsync(CancellationToken.None);

    // assert
    result.Select(d => d.Version).ShouldBe([2ul, 0ul]);
    result.ShouldAllBe(d => d.Name == nameof(TestEvent));
  }
}
