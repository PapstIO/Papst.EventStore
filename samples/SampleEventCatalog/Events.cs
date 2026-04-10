using Papst.EventStore.Aggregation.EventRegistration;

namespace SampleEventCatalog;

// Entity types
public record User(Guid Id, string Name, string Email, bool IsActive);
public record Order(Guid Id, Guid UserId, decimal Total, OrderStatus Status, List<OrderItem> Items);
public record OrderItem(string ProductName, int Quantity, decimal Price);

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}

// User events
[EventName<User>("UserCreated", Description = "Raised when a new user is created", Constraints = new[] { "Create" })]
public record UserCreatedEvent(string Name, string Email);

[EventName<User>("UserRenamed", Description = "Raised when a user changes their name", Constraints = new[] { "Update" })]
public record UserRenamedEvent(string NewName);

[EventName<User>("UserDeactivated", Description = "Raised when a user is deactivated", Constraints = new[] { "Update", "Admin" })]
public record UserDeactivatedEvent(string Reason);

// Order events
[EventName<Order>("OrderPlaced", Description = "Raised when a new order is placed", Constraints = new[] { "Create" })]
public record OrderPlacedEvent(Guid UserId, List<OrderItem> Items, decimal Total);

[EventName<Order>("OrderStatusChanged", Description = "Raised when order status changes", Constraints = new[] { "Update" })]
public record OrderStatusChangedEvent(OrderStatus NewStatus);

[EventName<Order>("OrderCancelled", Description = "Raised when an order is cancelled", Constraints = new[] { "Delete" })]
public record OrderCancelledEvent(string CancellationReason, DateTime CancelledAt);
