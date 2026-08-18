using Papst.EventStore.Aggregation.EventRegistration;

namespace SampleInMemoryAspNetApi.Orders;

[EventName<Order>("OrderPlaced")]
public sealed record OrderPlacedEvent(Guid OrderId, Guid UserId, List<OrderItem> Items, decimal Total);

[EventName<Order>("OrderStatusChanged")]
public sealed record OrderStatusChangedEvent(OrderStatus Status);

[EventName<Order>("OrderCancelled")]
public sealed record OrderCancelledEvent(string Reason);

// Uses the attribute-based aggregation: no hand-written aggregator is required. The source generator maps the
// event properties onto the equally named Order properties (Status, DeliveryTrackingCode, PickupDate,
// EstimatedArrivalDate).
[EventName<Order>("OrderShipped")]
[EventAggregation<Order>]
public sealed record OrderShippedEvent(
  OrderStatus Status,
  string DeliveryTrackingCode,
  DateTimeOffset PickupDate,
  DateTimeOffset EstimatedArrivalDate);
