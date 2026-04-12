using Papst.EventStore.Aggregation;
using Papst.EventStore;

namespace SampleInMemoryAspNetApi.Orders;

public sealed class OrderPlacedEventAggregator : EventAggregatorBase<Order, OrderPlacedEvent>
{
  public override ValueTask<Order?> ApplyAsync(OrderPlacedEvent evt, Order entity, IAggregatorStreamContext ctx)
  {
    entity.Id = evt.OrderId;
    entity.UserId = evt.UserId;
    entity.Total = evt.Total;
    entity.Status = OrderStatus.Pending;
    entity.CancellationReason = null;
    entity.Items = [.. evt.Items];

    return AsTask(entity);
  }
}

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
