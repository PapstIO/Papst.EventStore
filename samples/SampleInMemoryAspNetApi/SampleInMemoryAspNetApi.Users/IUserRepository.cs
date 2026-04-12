namespace SampleInMemoryAspNetApi.Users;

public interface IUserRepository
{
  ValueTask<User?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
  ValueTask UpsertAsync(User user, CancellationToken cancellationToken = default);
}
