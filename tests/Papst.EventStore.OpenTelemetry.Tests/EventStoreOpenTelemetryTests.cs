using System.Diagnostics;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore;
using Papst.EventStore.Aggregation.EventRegistration;
using Papst.EventStore.EventRegistration;
using Papst.EventStore.InMemory;
using Papst.EventStore.OpenTelemetry;
using Xunit;

namespace Papst.EventStore.OpenTelemetry.Tests;

[EventName("OtelTestEvent")]
public record OtelTestEvent
{
  public string Value { get; init; } = Guid.NewGuid().ToString();
}

public class EventStoreOpenTelemetryTests
{
  private static IServiceProvider BuildServiceProvider()
  {
    EventDescriptionEventRegistration registration = new();
    registration.AddEvent<OtelTestEvent>(new EventAttributeDescriptor(nameof(OtelTestEvent), true));

    return new ServiceCollection()
      .AddInMemoryEventStore()
      .AddEventStoreInstrumentation()
      .AddEventRegistrationTypeProvider()
      .AddSingleton<IEventRegistration>(registration)
      .AddLogging()
      .BuildServiceProvider();
  }

  [Fact]
  public void AddEventStoreInstrumentation_ShouldWrapEventStore()
  {
    // arrange & act
    var sp = BuildServiceProvider();

    // assert - the resolved IEventStore should NOT be the raw InMemory implementation
    var store = sp.GetRequiredService<IEventStore>();
    store.Should().NotBeOfType<InMemoryEventStore>();
    store.GetType().Name.Should().Be("InstrumentedEventStore");
  }

  [Fact]
  public void AddEventStoreInstrumentation_WithNoEventStore_ShouldThrow()
  {
    // arrange
    var services = new ServiceCollection();

    // act & assert
    var act = () => services.AddEventStoreInstrumentation();
    act.Should().Throw<InvalidOperationException>();
  }

  [Theory, AutoData]
  public async Task GetAsync_ShouldProduceActivity(Guid streamId)
  {
    // arrange
    var sp = BuildServiceProvider();
    var store = sp.GetRequiredService<IEventStore>();
    await store.CreateAsync(streamId, "TestTarget", CancellationToken.None);

    var activities = new List<Activity>();
    using var listener = new ActivityListener
    {
      ShouldListenTo = src => src.Name == EventStoreActivitySource.SourceName,
      Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
      ActivityStarted = activities.Add
    };
    ActivitySource.AddActivityListener(listener);

    // act
    await store.GetAsync(streamId, CancellationToken.None);

    // assert
    activities.Should().ContainSingle(a => a.OperationName == "EventStore.Get");
    var activity = activities.Single(a => a.OperationName == "EventStore.Get");
    activity.GetTagItem("event_store.stream_id").Should().Be(streamId.ToString());
  }

  [Theory, AutoData]
  public async Task CreateAsync_ShouldProduceActivity(Guid streamId)
  {
    // arrange
    var sp = BuildServiceProvider();
    var store = sp.GetRequiredService<IEventStore>();

    var activities = new List<Activity>();
    using var listener = new ActivityListener
    {
      ShouldListenTo = src => src.Name == EventStoreActivitySource.SourceName,
      Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
      ActivityStarted = activities.Add
    };
    ActivitySource.AddActivityListener(listener);

    // act
    await store.CreateAsync(streamId, "TestTarget", CancellationToken.None);

    // assert
    activities.Should().ContainSingle(a => a.OperationName == "EventStore.Create");
    var activity = activities.Single(a => a.OperationName == "EventStore.Create");
    activity.GetTagItem("event_store.stream_id").Should().Be(streamId.ToString());
    activity.GetTagItem("event_store.target_type").Should().Be("TestTarget");
  }

  [Theory, AutoData]
  public async Task AppendAsync_ShouldProduceActivity(Guid streamId, Guid eventId)
  {
    // arrange
    var sp = BuildServiceProvider();
    var store = sp.GetRequiredService<IEventStore>();
    var stream = await store.CreateAsync(streamId, "TestTarget", CancellationToken.None);

    var activities = new List<Activity>();
    using var listener = new ActivityListener
    {
      ShouldListenTo = src => src.Name == EventStoreActivitySource.SourceName,
      Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
      ActivityStarted = activities.Add
    };
    ActivitySource.AddActivityListener(listener);

    // act
    await stream.AppendAsync(eventId, new OtelTestEvent(), cancellationToken: CancellationToken.None);

    // assert
    activities.Should().ContainSingle(a => a.OperationName == "EventStream.Append");
    var activity = activities.Single(a => a.OperationName == "EventStream.Append");
    activity.GetTagItem("event_store.stream_id").Should().Be(streamId.ToString());
    activity.GetTagItem("event_store.event_id").Should().Be(eventId.ToString());
  }

  [Theory, AutoData]
  public async Task ListAsync_ShouldProduceActivity(Guid streamId)
  {
    // arrange
    var sp = BuildServiceProvider();
    var store = sp.GetRequiredService<IEventStore>();
    var stream = await store.CreateAsync(streamId, "TestTarget", CancellationToken.None);
    await stream.AppendAsync(Guid.NewGuid(), new OtelTestEvent(), cancellationToken: CancellationToken.None);

    var activities = new List<Activity>();
    using var listener = new ActivityListener
    {
      ShouldListenTo = src => src.Name == EventStoreActivitySource.SourceName,
      Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
      ActivityStarted = activities.Add
    };
    ActivitySource.AddActivityListener(listener);

    // act
    var docs = await stream.ListAsync(0, CancellationToken.None).ToListAsync(CancellationToken.None);

    // assert
    docs.Should().HaveCount(1);
    activities.Should().ContainSingle(a => a.OperationName == "EventStream.List");
    var activity = activities.Single(a => a.OperationName == "EventStream.List");
    activity.GetTagItem("event_store.stream_id").Should().Be(streamId.ToString());
    activity.GetTagItem("event_store.document_count").Should().Be("1");
  }

  [Theory, AutoData]
  public async Task CommitAsync_ShouldProduceActivity(Guid streamId)
  {
    // arrange
    var sp = BuildServiceProvider();
    var store = sp.GetRequiredService<IEventStore>();
    var stream = await store.CreateAsync(streamId, "TestTarget", CancellationToken.None);
    // Append an event first so the transactional batch can calculate version from a non-empty list
    await stream.AppendAsync(Guid.NewGuid(), new OtelTestEvent(), cancellationToken: CancellationToken.None);
    var batch = await stream.CreateTransactionalBatchAsync();
    batch.Add(Guid.NewGuid(), new OtelTestEvent());

    var activities = new List<Activity>();
    using var listener = new ActivityListener
    {
      ShouldListenTo = src => src.Name == EventStoreActivitySource.SourceName,
      Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
      ActivityStarted = activities.Add
    };
    ActivitySource.AddActivityListener(listener);

    // act
    await batch.CommitAsync(CancellationToken.None);

    // assert
    activities.Should().ContainSingle(a => a.OperationName == "EventStore.TransactionAppender.Commit");
    var activity = activities.Single(a => a.OperationName == "EventStore.TransactionAppender.Commit");
    activity.GetTagItem("event_store.stream_id").Should().Be(streamId.ToString());
  }
}
