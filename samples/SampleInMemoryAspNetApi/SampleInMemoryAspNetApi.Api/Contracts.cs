using SampleInMemoryAspNetApi.Orders;

namespace SampleInMemoryAspNetApi.Api;

public sealed record CreateUserRequest(string Name, string Email);
public sealed record RenameUserRequest(string Name);
public sealed record DeactivateUserRequest(string Reason);
public sealed record CreateOrderRequest(Guid UserId, List<CreateOrderItemRequest> Items);
public sealed record CreateOrderItemRequest(string ProductName, int Quantity, decimal UnitPrice);
public sealed record ChangeOrderStatusRequest(OrderStatus Status);
public sealed record CancelOrderRequest(string Reason);
public sealed record CatalogEventResponse(string EventName, string? Description, string[]? Constraints);
public sealed record CatalogEventDetailsResponse(string EventName, string? Description, string[]? Constraints, string JsonSchema);
