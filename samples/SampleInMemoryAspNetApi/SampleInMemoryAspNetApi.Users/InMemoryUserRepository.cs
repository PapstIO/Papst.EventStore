using System.Collections.Concurrent;

namespace SampleInMemoryAspNetApi.Users;

public sealed class InMemoryUserRepository : IUserRepository
{
  private readonly ConcurrentDictionary<Guid, User> _users = new();

  public ValueTask<User?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
  {
    _users.TryGetValue(userId, out User? user);
    return ValueTask.FromResult(user is null ? null : Clone(user));
  }

  public ValueTask UpsertAsync(User user, CancellationToken cancellationToken = default)
  {
    _users[user.Id] = Clone(user);
    return ValueTask.CompletedTask;
  }

  private static User Clone(User user)
  {
    return new User
    {
      Id = user.Id,
      Name = user.Name,
      Email = user.Email,
      IsActive = user.IsActive,
      DeactivationReason = user.DeactivationReason,
      Version = user.Version
    };
  }
}
