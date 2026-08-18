#nullable enable
using System.Collections.Generic;
using Papst.EventStore.Aggregation.EventRegistration;

namespace Papst.EventStore.Tests.Aggregation;

/// <summary>
/// Sample aggregate + events used to exercise the code-generated attribute based aggregation
/// end to end (store agnostic). Events carry both <c>[EventName]</c> (for type resolution) and
/// <c>[EventAggregation]</c> (for the generated aggregator).
/// </summary>
public class OrderAggregate : IEntity
{
  public ulong Version { get; set; }
  public string? CustomerName { get; set; }
  public OrderAddress ShippingAddress { get; set; } = new();
  public Dictionary<string, OrderLine> Lines { get; set; } = new();
  public List<OrderTag> Tags { get; set; } = new();
}

public class OrderAddress
{
  public string? City { get; set; }
  public string? Zip { get; set; }
}

public class OrderLine
{
  public int Quantity { get; set; }
  public string? Note { get; set; }
}

public class OrderTag
{
  public string? Id { get; set; }
  public string? Label { get; set; }
}

[EventName(nameof(OrderCreated))]
[EventAggregation<OrderAggregate>]
public record OrderCreated(string? CustomerName);

[EventName(nameof(CustomerNameForced))]
[EventAggregation<OrderAggregate>(SkipNullValues = false)]
public record CustomerNameForced(string? CustomerName);

[EventName(nameof(ShippingAddressSet))]
[EventAggregation<OrderAggregate>(PropertyPath = nameof(OrderAggregate.ShippingAddress))]
public record ShippingAddressSet(string? City, string? Zip);

[EventName(nameof(LineUpserted))]
[EventAggregation<OrderAggregate>(PropertyPath = nameof(OrderAggregate.Lines))]
public record LineUpserted([property: AggregationDictionaryKey] string Sku, int Quantity, [property: SkipWhenNull(true)] string? Note);

[EventName(nameof(TagUpserted))]
[EventAggregation<OrderAggregate>(PropertyPath = nameof(OrderAggregate.Tags))]
public record TagUpserted([property: AggregationCollectionKey("Id")] string TagId, string? Label);
