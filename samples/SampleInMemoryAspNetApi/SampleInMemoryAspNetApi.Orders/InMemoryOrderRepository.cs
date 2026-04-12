using System.Collections.Concurrent;

namespace SampleInMemoryAspNetApi.Orders;

public sealed class InMemoryOrderRepository : IOrderRepository
{
  private readonly ConcurrentDictionary<Guid, Order> _orders = new();

  public ValueTask<Order?> GetAsync(Guid orderId, CancellationToken cancellationToken = default)
  {
    _orders.TryGetValue(orderId, out Order? order);
    return ValueTask.FromResult(order is null ? null : Clone(order));
  }

  public ValueTask UpsertAsync(Order order, CancellationToken cancellationToken = default)
  {
    _orders[order.Id] = Clone(order);
    return ValueTask.CompletedTask;
  }

  private static Order Clone(Order order)
  {
    return new Order
    {
      Id = order.Id,
      UserId = order.UserId,
      Total = order.Total,
      Status = order.Status,
      CancellationReason = order.CancellationReason,
      Version = order.Version,
      Items = [.. order.Items]
    };
  }
}
