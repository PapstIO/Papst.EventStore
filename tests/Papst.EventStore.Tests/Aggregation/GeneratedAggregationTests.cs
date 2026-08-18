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
using Shouldly;
using Xunit;

namespace Papst.EventStore.Tests.Aggregation;

/// <summary>
/// End-to-end, store-agnostic tests for the code-generated attribute based aggregation. They drive the real
/// generated aggregators (registered via the generated <c>AddCodeGeneratedEvents</c>) through the
/// <see cref="IEventStreamAggregator{TEntity}"/> using a hand-rolled stream.
/// </summary>
public class GeneratedAggregationTests
{
  private static IEventStreamAggregator<OrderAggregate> BuildAggregator()
  {
    var services = new ServiceCollection();
    services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
    services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
    services.AddRegisteredEventAggregation();
    // generated extension: registers [EventName] events, the type provider and all generated aggregators
    EventStoreEventAggregator.AddCodeGeneratedEvents(services);
    return services.BuildServiceProvider().GetRequiredService<IEventStreamAggregator<OrderAggregate>>();
  }

  [Fact]
  public async Task RootAggregation_MapsMatchingProperties()
  {
    var aggregator = BuildAggregator();
    var stream = new FakeStream();
    stream.Append(new OrderCreated("Alice"));

    var order = await aggregator.AggregateAsync(stream, CancellationToken.None);

    order.ShouldNotBeNull();
    order!.CustomerName.ShouldBe("Alice");
  }

  [Fact]
  public async Task RootAggregation_SkipsNullByDefault_ButForcesWhenConfigured()
  {
    var aggregator = BuildAggregator();
    var stream = new FakeStream();
    stream.Append(new OrderCreated("Alice"));
    stream.Append(new OrderCreated(null));          // skip-null default: keeps "Alice"

    var order = await aggregator.AggregateAsync(stream, CancellationToken.None);
    order!.CustomerName.ShouldBe("Alice");

    stream.Append(new CustomerNameForced(null));    // SkipNullValues = false: clears
    order = await aggregator.AggregateAsync(stream, CancellationToken.None);
    order!.CustomerName.ShouldBeNull();
  }

  [Fact]
  public async Task NestedPropertyPath_AggregatesOntoChild()
  {
    var aggregator = BuildAggregator();
    var stream = new FakeStream();
    stream.Append(new ShippingAddressSet("Berlin", "10115"));

    var order = await aggregator.AggregateAsync(stream, CancellationToken.None);

    order!.ShippingAddress.City.ShouldBe("Berlin");
    order.ShippingAddress.Zip.ShouldBe("10115");
  }

  [Fact]
  public async Task DictionaryPropertyPath_UpsertsEntries()
  {
    var aggregator = BuildAggregator();
    var stream = new FakeStream();
    stream.Append(new LineUpserted("SKU-1", 2, "first"));
    stream.Append(new LineUpserted("SKU-2", 5, null));
    stream.Append(new LineUpserted("SKU-1", 9, null));  // updates existing; Note skipped (null)

    var order = await aggregator.AggregateAsync(stream, CancellationToken.None);

    order!.Lines.Count.ShouldBe(2);
    order.Lines["SKU-1"].Quantity.ShouldBe(9);
    order.Lines["SKU-1"].Note.ShouldBe("first");   // preserved because SkipWhenNull(true)
    order.Lines["SKU-2"].Quantity.ShouldBe(5);
  }

  [Fact]
  public async Task CollectionPropertyPath_UpsertsBySearchKey()
  {
    var aggregator = BuildAggregator();
    var stream = new FakeStream();
    stream.Append(new TagUpserted("t1", "Urgent"));
    stream.Append(new TagUpserted("t2", "Wholesale"));
    stream.Append(new TagUpserted("t1", "Priority"));   // updates existing item

    var order = await aggregator.AggregateAsync(stream, CancellationToken.None);

    order!.Tags.Count.ShouldBe(2);
    order.Tags.ShouldContain(t => t.Id == "t1" && t.Label == "Priority");
    order.Tags.ShouldContain(t => t.Id == "t2" && t.Label == "Wholesale");
  }

  private sealed class FakeStream : IEventStream
  {
    private readonly List<EventStreamDocument> _events = new();

    public Guid StreamId { get; } = Guid.NewGuid();
    public ulong Version => _events.Count == 0 ? 0 : _events[_events.Count - 1].Version;
    public DateTimeOffset Created { get; } = DateTimeOffset.UtcNow;
    public ulong? LatestSnapshotVersion => null;
    public EventStreamMetaData MetaData { get; } = new();

    public void Append<TEvent>(TEvent evt) where TEvent : notnull
      => _events.Add(new EventStreamDocument
      {
        Id = Guid.NewGuid(),
        StreamId = StreamId,
        DocumentType = EventStreamDocumentType.Event,
        Version = (ulong)_events.Count,
        Time = DateTimeOffset.UtcNow,
        Name = typeof(TEvent).Name,
        Data = JObject.FromObject(evt),
        DataType = typeof(TEvent).Name,
        TargetType = nameof(OrderAggregate),
      });

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
