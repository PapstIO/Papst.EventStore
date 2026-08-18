using Papst.EventStore;

namespace SampleInMemoryAspNetApi.Orders;

public sealed class Order : IEntity
{
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

  public ulong Version { get; set; }
}

public sealed record OrderItem(string ProductName, int Quantity, decimal UnitPrice);

public enum OrderStatus
{
  Pending = 0,
  Confirmed = 1,
  Shipped = 2,
  Cancelled = 3
}
