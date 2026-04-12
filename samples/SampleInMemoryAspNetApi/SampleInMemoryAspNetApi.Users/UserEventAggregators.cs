using Papst.EventStore.Aggregation;
using Papst.EventStore;

namespace SampleInMemoryAspNetApi.Users;

public sealed class UserRegisteredEventAggregator : EventAggregatorBase<User, UserRegisteredEvent>
{
  public override ValueTask<User?> ApplyAsync(UserRegisteredEvent evt, User entity, IAggregatorStreamContext ctx)
  {
    entity.Id = evt.UserId;
    entity.Name = evt.Name;
    entity.Email = evt.Email;
    entity.IsActive = true;
    entity.DeactivationReason = null;

    return AsTask(entity);
  }
}

public sealed class UserRenamedEventAggregator : EventAggregatorBase<User, UserRenamedEvent>
{
  public override ValueTask<User?> ApplyAsync(UserRenamedEvent evt, User entity, IAggregatorStreamContext ctx)
  {
    entity.Name = evt.Name;
    return AsTask(entity);
  }
}

public sealed class UserDeactivatedEventAggregator : EventAggregatorBase<User, UserDeactivatedEvent>
{
  public override ValueTask<User?> ApplyAsync(UserDeactivatedEvent evt, User entity, IAggregatorStreamContext ctx)
  {
    entity.IsActive = false;
    entity.DeactivationReason = evt.Reason;
    return AsTask(entity);
  }
}
