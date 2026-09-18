using Papst.EventStore.Aggregation.EventRegistration;

namespace SampleInMemoryAspNetApi.Orders;

// Attribute-aggregated create event: UserId/Items/Total are mapped by name, Id/Created are stamped from the stream
// context (create-only), Updated on every event. OrderId is ignored because Id is stamped from the stream id.
[EventName<Order>("OrderPlaced")]
[EventAggregation<Order>]
public sealed record OrderPlacedEvent(
  [property: AggregationIgnore] Guid OrderId,
  Guid UserId,
  List<OrderItem> Items,
  decimal Total);

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

// Attribute-based collection upsert: the Item whose Id matches is updated, otherwise a new one is added.
[EventName<Order>("OrderItemUpserted")]
[EventAggregation<Order>(PropertyPath = nameof(Order.Items))]
public sealed record OrderItemUpsertedEvent(
  [property: AggregationCollectionKey(nameof(OrderItem.Id))] Guid Id,
  string ProductName,
  int Quantity,
  decimal UnitPrice);

// Attribute-based collection remove-by-key: the Item whose Id matches is removed from the collection.
[EventName<Order>("OrderItemRemoved")]
[EventAggregation<Order>(PropertyPath = nameof(Order.Items), Mode = AggregationMode.RemoveByKey)]
public sealed record OrderItemRemovedEvent(
  [property: AggregationCollectionKey(nameof(OrderItem.Id))] Guid Id);
