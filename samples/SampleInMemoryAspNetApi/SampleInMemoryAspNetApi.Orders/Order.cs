using Papst.EventStore;
using Papst.EventStore.Aggregation.EventRegistration;

namespace SampleInMemoryAspNetApi.Orders;

public sealed class Order : IEntity
{
  // Id is stamped from the stream id on the create event (no explicit assignment required).
  [AggregationContextStamp(AggregationContextValue.StreamId, OnEveryEvent = false)]
  public Guid Id { get; set; }

  public Guid UserId { get; set; }
  public decimal Total { get; set; }
  public OrderStatus Status { get; set; }
  public string? CancellationReason { get; set; }
  public List<OrderItem> Items { get; set; } = [];

  // Shipping information, populated by the attribute-aggregated OrderShippedEvent.
  public string? DeliveryTrackingCode { get; set; }
  public DateTimeOffset? PickupDate { get; set; }
  public DateTimeOffset? EstimatedArrivalDate { get; set; }

  // Audit fields stamped by the source generator from the stream context: Created once (on the create event),
  // Updated on every attribute-aggregated event.
  [AggregationContextStamp(AggregationContextValue.StreamCreated, OnEveryEvent = false)]
  public DateTimeOffset Created { get; set; }

  [AggregationContextStamp(AggregationContextValue.EventTime)]
  public DateTimeOffset Updated { get; set; }

  public ulong Version { get; set; }
}

// A keyed item so it can be upserted and removed by the attribute-based collection aggregation. A parameterless
// constructor and settable properties are required for the generator to create/populate items.
public sealed class OrderItem
{
  public Guid Id { get; set; }
  public string ProductName { get; set; } = string.Empty;
  public int Quantity { get; set; }
  public decimal UnitPrice { get; set; }
}

public enum OrderStatus
{
  Pending = 0,
  Confirmed = 1,
  Shipped = 2,
  Cancelled = 3
}
