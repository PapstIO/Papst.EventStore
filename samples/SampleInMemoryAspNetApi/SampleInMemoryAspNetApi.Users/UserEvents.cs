using Papst.EventStore.Aggregation.EventRegistration;

namespace SampleInMemoryAspNetApi.Users;

[EventName<User>("UserRegistered")]
public sealed record UserRegisteredEvent(Guid UserId, string Name, string Email);

[EventName<User>("UserRenamed")]
public sealed record UserRenamedEvent(string Name);

[EventName<User>("UserDeactivated")]
public sealed record UserDeactivatedEvent(string Reason);
