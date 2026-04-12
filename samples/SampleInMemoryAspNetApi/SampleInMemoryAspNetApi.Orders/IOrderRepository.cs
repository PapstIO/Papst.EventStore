namespace SampleInMemoryAspNetApi.Orders;

public interface IOrderRepository
{
  ValueTask<Order?> GetAsync(Guid orderId, CancellationToken cancellationToken = default);
  ValueTask UpsertAsync(Order order, CancellationToken cancellationToken = default);
}
