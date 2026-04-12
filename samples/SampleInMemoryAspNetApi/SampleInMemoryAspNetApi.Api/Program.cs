using Papst.EventStore;
using Papst.EventStore.Aggregation;
using Papst.EventStore.Aggregation.EventRegistration;
using Papst.EventStore.EventCatalog;
using Papst.EventStore.Exceptions;
using Papst.EventStore.InMemory;
using SampleInMemoryAspNetApi.Api;
using SampleInMemoryAspNetApi.Orders;
using SampleInMemoryAspNetApi.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInMemoryEventStore();
builder.Services.AddRegisteredEventAggregation();
SampleInMemoryAspNetApi.Users.EventStoreEventAggregator.AddCodeGeneratedEvents(builder.Services);
SampleInMemoryAspNetApi.Orders.EventStoreEventAggregator.AddCodeGeneratedEvents(builder.Services);
SampleInMemoryAspNetApi.Users.EventStoreEventAggregator.AddCodeGeneratedEventCatalog(builder.Services);
SampleInMemoryAspNetApi.Orders.EventStoreEventAggregator.AddCodeGeneratedEventCatalog(builder.Services);
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Ok(new
{
  message = "Sample in-memory event sourced API",
  endpoints = new[]
  {
    "POST /users",
    "POST /users/{userId}/rename",
    "POST /users/{userId}/deactivate",
    "GET /users/{userId}",
    "POST /orders",
    "POST /orders/{orderId}/status",
    "POST /orders/{orderId}/cancel",
    "GET /orders/{orderId}",
    "GET /catalog/{entity}/events",
    "GET /catalog/{entity}/events/{eventName}/schema",
    "GET /swagger"
  }
}));

RouteGroupBuilder users = app.MapGroup("/users");
users.MapPost("/", CreateUserAsync);
users.MapPost("/{userId:guid}/rename", RenameUserAsync);
users.MapPost("/{userId:guid}/deactivate", DeactivateUserAsync);
users.MapGet("/{userId:guid}", GetUserAsync);

RouteGroupBuilder orders = app.MapGroup("/orders");
orders.MapPost("/", CreateOrderAsync);
orders.MapPost("/{orderId:guid}/status", ChangeOrderStatusAsync);
orders.MapPost("/{orderId:guid}/cancel", CancelOrderAsync);
orders.MapGet("/{orderId:guid}", GetOrderAsync);

RouteGroupBuilder catalog = app.MapGroup("/catalog");
catalog.MapGet("/{entity}/events", ListCatalogEventsAsync);
catalog.MapGet("/{entity}/events/{eventName}/schema", GetCatalogEventSchemaAsync);

app.Run();

static async Task<IResult> CreateUserAsync(
  CreateUserRequest request,
  IEventStore eventStore,
  IEventStreamAggregator<User> aggregator,
  IUserRepository repository,
  CancellationToken cancellationToken)
{
  Guid userId = Guid.NewGuid();
  IEventStream stream = await eventStore.CreateAsync(userId, nameof(User), cancellationToken);
  await stream.AppendAsync(Guid.NewGuid(), new UserRegisteredEvent(userId, request.Name, request.Email), cancellationToken: cancellationToken);

  User? user = await AggregateAndStoreAsync(
    stream,
    aggregator,
    repository.UpsertAsync,
    cancellationToken);

  return user is null
    ? Results.Problem("User aggregation returned no entity.")
    : Results.Created($"/users/{userId}", user);
}

static async Task<IResult> RenameUserAsync(
  Guid userId,
  RenameUserRequest request,
  IEventStore eventStore,
  IEventStreamAggregator<User> aggregator,
  IUserRepository repository,
  CancellationToken cancellationToken)
{
  IEventStream? stream = await TryGetStreamAsync(eventStore, userId, cancellationToken);
  if (stream is null)
  {
    return Results.NotFound();
  }

  await stream.AppendAsync(Guid.NewGuid(), new UserRenamedEvent(request.Name), cancellationToken: cancellationToken);
  User? user = await AggregateAndStoreAsync(stream, aggregator, repository.UpsertAsync, cancellationToken);

  return user is null
    ? Results.Problem("User aggregation returned no entity.")
    : Results.Ok(user);
}

static async Task<IResult> DeactivateUserAsync(
  Guid userId,
  DeactivateUserRequest request,
  IEventStore eventStore,
  IEventStreamAggregator<User> aggregator,
  IUserRepository repository,
  CancellationToken cancellationToken)
{
  IEventStream? stream = await TryGetStreamAsync(eventStore, userId, cancellationToken);
  if (stream is null)
  {
    return Results.NotFound();
  }

  await stream.AppendAsync(Guid.NewGuid(), new UserDeactivatedEvent(request.Reason), cancellationToken: cancellationToken);
  User? user = await AggregateAndStoreAsync(stream, aggregator, repository.UpsertAsync, cancellationToken);

  return user is null
    ? Results.Problem("User aggregation returned no entity.")
    : Results.Ok(user);
}

static async Task<IResult> GetUserAsync(
  Guid userId,
  IUserRepository repository,
  CancellationToken cancellationToken)
{
  User? user = await repository.GetAsync(userId, cancellationToken);
  return user is null ? Results.NotFound() : Results.Ok(user);
}

static async Task<IResult> CreateOrderAsync(
  CreateOrderRequest request,
  IEventStore eventStore,
  IEventStreamAggregator<Order> aggregator,
  IUserRepository userRepository,
  IOrderRepository orderRepository,
  CancellationToken cancellationToken)
{
  User? user = await userRepository.GetAsync(request.UserId, cancellationToken);
  if (user is null)
  {
    return Results.BadRequest(new { message = $"User '{request.UserId}' was not found." });
  }

  Guid orderId = Guid.NewGuid();
  IEventStream stream = await eventStore.CreateAsync(orderId, nameof(Order), cancellationToken);

  List<OrderItem> items = request.Items
    .Select(item => new OrderItem(item.ProductName, item.Quantity, item.UnitPrice))
    .ToList();

  decimal total = items.Sum(item => item.Quantity * item.UnitPrice);

  await stream.AppendAsync(
    Guid.NewGuid(),
    new OrderPlacedEvent(orderId, request.UserId, items, total),
    cancellationToken: cancellationToken);

  Order? order = await AggregateAndStoreAsync(
    stream,
    aggregator,
    orderRepository.UpsertAsync,
    cancellationToken);

  return order is null
    ? Results.Problem("Order aggregation returned no entity.")
    : Results.Created($"/orders/{orderId}", order);
}

static async Task<IResult> ChangeOrderStatusAsync(
  Guid orderId,
  ChangeOrderStatusRequest request,
  IEventStore eventStore,
  IEventStreamAggregator<Order> aggregator,
  IOrderRepository repository,
  CancellationToken cancellationToken)
{
  IEventStream? stream = await TryGetStreamAsync(eventStore, orderId, cancellationToken);
  if (stream is null)
  {
    return Results.NotFound();
  }

  await stream.AppendAsync(Guid.NewGuid(), new OrderStatusChangedEvent(request.Status), cancellationToken: cancellationToken);
  Order? order = await AggregateAndStoreAsync(stream, aggregator, repository.UpsertAsync, cancellationToken);

  return order is null
    ? Results.Problem("Order aggregation returned no entity.")
    : Results.Ok(order);
}

static async Task<IResult> CancelOrderAsync(
  Guid orderId,
  CancelOrderRequest request,
  IEventStore eventStore,
  IEventStreamAggregator<Order> aggregator,
  IOrderRepository repository,
  CancellationToken cancellationToken)
{
  IEventStream? stream = await TryGetStreamAsync(eventStore, orderId, cancellationToken);
  if (stream is null)
  {
    return Results.NotFound();
  }

  await stream.AppendAsync(Guid.NewGuid(), new OrderCancelledEvent(request.Reason), cancellationToken: cancellationToken);
  Order? order = await AggregateAndStoreAsync(stream, aggregator, repository.UpsertAsync, cancellationToken);

  return order is null
    ? Results.Problem("Order aggregation returned no entity.")
    : Results.Ok(order);
}

static async Task<IResult> GetOrderAsync(
  Guid orderId,
  IOrderRepository repository,
  CancellationToken cancellationToken)
{
  Order? order = await repository.GetAsync(orderId, cancellationToken);
  return order is null ? Results.NotFound() : Results.Ok(order);
}

static async Task<IResult> ListCatalogEventsAsync(
  string entity,
  IEventCatalog catalog,
  CancellationToken cancellationToken)
{
  if (TryResolveCatalogEntity(entity) is not CatalogEntity catalogEntity)
  {
    return Results.NotFound();
  }

  IReadOnlyList<CatalogEventResponse> events = catalogEntity switch
  {
    CatalogEntity.Users => (await catalog.ListEvents<User>()).Select(MapCatalogEvent).ToArray(),
    CatalogEntity.Orders => (await catalog.ListEvents<Order>()).Select(MapCatalogEvent).ToArray(),
    _ => []
  };

  return Results.Ok(new
  {
    entity = entity.ToLowerInvariant(),
    events
  });
}

static async Task<IResult> GetCatalogEventSchemaAsync(
  string entity,
  string eventName,
  IEventCatalog catalog,
  CancellationToken cancellationToken)
{
  if (TryResolveCatalogEntity(entity) is not CatalogEntity catalogEntity)
  {
    return Results.NotFound();
  }

  EventCatalogEventDetails? details = catalogEntity switch
  {
    CatalogEntity.Users => await catalog.GetEventDetails<User>(eventName),
    CatalogEntity.Orders => await catalog.GetEventDetails<Order>(eventName),
    _ => null
  };

  if (details is null)
  {
    return Results.NotFound();
  }

  return Results.Ok(MapCatalogEventDetails(details));
}

static async Task<TEntity?> AggregateAndStoreAsync<TEntity>(
  IEventStream stream,
  IEventStreamAggregator<TEntity> aggregator,
  Func<TEntity, CancellationToken, ValueTask> store,
  CancellationToken cancellationToken)
  where TEntity : class, new()
{
  TEntity? entity = await aggregator.AggregateAsync(stream, cancellationToken);
  if (entity is not null)
  {
    await store(entity, cancellationToken);
  }

  return entity;
}

static async Task<IEventStream?> TryGetStreamAsync(
  IEventStore eventStore,
  Guid streamId,
  CancellationToken cancellationToken)
{
  try
  {
    return await eventStore.GetAsync(streamId, cancellationToken);
  }
  catch (EventStreamNotFoundException)
  {
    return null;
  }
}

static CatalogEntity? TryResolveCatalogEntity(string entity)
{
  return entity.ToLowerInvariant() switch
  {
    "users" => CatalogEntity.Users,
    "orders" => CatalogEntity.Orders,
    _ => null
  };
}

static CatalogEventResponse MapCatalogEvent(EventCatalogEntry entry)
{
  return new CatalogEventResponse(entry.EventName, entry.Description, entry.Constraints);
}

static CatalogEventDetailsResponse MapCatalogEventDetails(EventCatalogEventDetails details)
{
  return new CatalogEventDetailsResponse(details.EventName, details.Description, details.Constraints, details.JsonSchema);
}

enum CatalogEntity
{
  Users,
  Orders
}
