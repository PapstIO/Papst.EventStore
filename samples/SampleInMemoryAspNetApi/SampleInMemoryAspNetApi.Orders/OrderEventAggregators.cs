using Papst.EventStore.Aggregation;
using Papst.EventStore;

namespace SampleInMemoryAspNetApi.Orders;

// OrderPlacedEvent is now handled by the code-generated attribute aggregation (see OrderEvents.cs).
// The status-change and cancellation events remain hand-written to show both mechanisms coexisting.

public sealed class OrderStatusChangedEventAggregator : EventAggregatorBase<Order, OrderStatusChangedEvent>
{
  public override ValueTask<Order?> ApplyAsync(OrderStatusChangedEvent evt, Order entity, IAggregatorStreamContext ctx)
  {
    entity.Status = evt.Status;
    return AsTask(entity);
  }
}

public sealed class OrderCancelledEventAggregator : EventAggregatorBase<Order, OrderCancelledEvent>
{
  public override ValueTask<Order?> ApplyAsync(OrderCancelledEvent evt, Order entity, IAggregatorStreamContext ctx)
  {
    entity.Status = OrderStatus.Cancelled;
    entity.CancellationReason = evt.Reason;
    return AsTask(entity);
  }
}
