#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Papst.EventStore.Aggregation;
using Papst.EventStore.Aggregation.EventRegistration;
using Papst.EventStore.Documents;
using Papst.EventStore.EventRegistration;
using Shouldly;
using Xunit;

namespace Papst.EventStore.Tests;

/// <summary>
/// Regression tests for the boundary-event re-application bug in
/// <see cref="EventRegistrationEventAggregator{TEntity}"/>. Uses a hand-rolled
/// IEventStream so the test does not depend on a specific storage adapter's
/// version-tracking semantics.
/// </summary>
public class EventRegistrationEventAggregatorTests
{
  [Fact]
  public async Task AggregateAsync_AppliesEachEventOnce_WhenContinuingFromExistingTarget()
  {
    var aggregator = BuildAggregator();
    var stream = new FakeEventStream();
    stream.Append(new IncrementEvent("a"));
    stream.Append(new IncrementEvent("b"));
    stream.Append(new IncrementEvent("c"));

    var entity = await aggregator.AggregateAsync(stream, CancellationToken.None);
    entity.ShouldNotBeNull();
    entity.Tags.ShouldBe(["a", "b", "c"]);

    stream.Append(new IncrementEvent("d"));
    stream.Append(new IncrementEvent("e"));

    entity = await aggregator.AggregateAsync(stream, entity, CancellationToken.None);

    entity.ShouldNotBeNull();
    entity.Tags.ShouldBe(["a", "b", "c", "d", "e"]);
  }

  [Fact]
  public async Task AggregateAsync_AppliesAllEvents_WhenAggregatingFromScratch()
  {
    var aggregator = BuildAggregator();
    var stream = new FakeEventStream();
    stream.Append(new IncrementEvent("a"));
    stream.Append(new IncrementEvent("b"));

    var entity = await aggregator.AggregateAsync(stream, CancellationToken.None);

    entity.ShouldNotBeNull();
    entity.Tags.ShouldBe(["a", "b"]);
  }

  private static IEventStreamAggregator<CountingEntity> BuildAggregator()
  {
    var registration = new EventDescriptionEventRegistration();
    registration.AddEvent<IncrementEvent>(new EventAttributeDescriptor(nameof(IncrementEvent), true));
    var typeProvider = new EventRegistrationTypeProvider(NullLogger<EventRegistrationTypeProvider>.Instance, [registration]);

    var services = new ServiceCollection();
    services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
    services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
    services.AddSingleton<IEventTypeProvider>(typeProvider);
    services.AddRegisteredEventAggregation();
    services.AddTransient<IEventAggregator<CountingEntity, IncrementEvent>, IncrementEventAggregator>();
    return services.BuildServiceProvider().GetRequiredService<IEventStreamAggregator<CountingEntity>>();
  }

  public class CountingEntity : IEntity
  {
    public Guid Id { get; set; }
    public ulong Version { get; set; }
    public List<string> Tags { get; set; } = [];
  }

  [EventName(nameof(IncrementEvent))]
  public record IncrementEvent(string Tag);

  public class IncrementEventAggregator : EventAggregatorBase<CountingEntity, IncrementEvent>
  {
    public override ValueTask<CountingEntity?> ApplyAsync(IncrementEvent evt, CountingEntity entity, IAggregatorStreamContext ctx)
    {
      entity.Tags.Add(evt.Tag);
      return AsTask(entity);
    }
  }

  private sealed class FakeEventStream : IEventStream
  {
    private readonly List<EventStreamDocument> _events = [];

    public Guid StreamId { get; } = Guid.NewGuid();
    public ulong Version => _events.Count == 0 ? 0 : _events[^1].Version;
    public DateTimeOffset Created { get; } = DateTimeOffset.UtcNow;
    public ulong? LatestSnapshotVersion => null;
    public EventStreamMetaData MetaData { get; } = new();

    public void Append<TEvent>(TEvent evt) where TEvent : notnull
    {
      _events.Add(new EventStreamDocument
      {
        Id = Guid.NewGuid(),
        StreamId = StreamId,
        DocumentType = EventStreamDocumentType.Event,
        Version = (ulong)_events.Count,
        Time = DateTimeOffset.UtcNow,
        Name = typeof(TEvent).Name,
        Data = JObject.FromObject(evt),
        DataType = typeof(TEvent).Name,
        TargetType = nameof(CountingEntity),
      });
    }

    public Task<EventStreamDocument?> GetLatestSnapshot(CancellationToken cancellationToken = default)
      => Task.FromResult<EventStreamDocument?>(null);

    public Task AppendAsync<TEvent>(Guid id, TEvent evt, EventStreamMetaData? metaData = null, CancellationToken cancellationToken = default) where TEvent : notnull
      => throw new NotSupportedException();

    public Task AppendSnapshotAsync<TEntity>(Guid id, TEntity entity, EventStreamMetaData? metaData = null, CancellationToken cancellationToken = default) where TEntity : notnull
      => throw new NotSupportedException();

    public Task<IEventStoreTransactionAppender> CreateTransactionalBatchAsync()
      => throw new NotSupportedException();

    public IAsyncEnumerable<EventStreamDocument> ListAsync(ulong startVersion = 0u, CancellationToken cancellationToken = default)
      => ListAsync(startVersion, Version, cancellationToken);

    public async IAsyncEnumerable<EventStreamDocument> ListAsync(ulong startVersion, ulong endVersion, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      foreach (var doc in _events)
      {
        if (doc.Version >= startVersion && doc.Version <= endVersion)
        {
          yield return doc;
        }
      }
      await Task.CompletedTask;
    }

    public IAsyncEnumerable<EventStreamDocument> ListDescendingAsync(ulong endVersion, ulong startVersion, CancellationToken cancellationToken = default)
      => throw new NotSupportedException();

    public IAsyncEnumerable<EventStreamDocument> ListDescendingAsync(ulong endVersion, CancellationToken cancellationToken = default)
      => throw new NotSupportedException();

    public Task UpdateStreamMetaData(EventStreamMetaData metaData, CancellationToken cancellationToken = default)
      => Task.CompletedTask;
  }
}
