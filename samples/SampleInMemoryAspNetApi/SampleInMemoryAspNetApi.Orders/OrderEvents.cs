using Papst.EventStore.Aggregation.EventRegistration;

namespace SampleInMemoryAspNetApi.Orders;

[EventName<Order>("OrderPlaced")]
public sealed record OrderPlacedEvent(Guid OrderId, Guid UserId, List<OrderItem> Items, decimal Total);

[EventName<Order>("OrderStatusChanged")]
public sealed record OrderStatusChangedEvent(OrderStatus Status);

[EventName<Order>("OrderCancelled")]
public sealed record OrderCancelledEvent(string Reason);
