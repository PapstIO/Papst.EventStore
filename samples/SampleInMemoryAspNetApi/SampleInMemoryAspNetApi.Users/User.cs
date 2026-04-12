using Papst.EventStore;

namespace SampleInMemoryAspNetApi.Users;

public sealed class User : IEntity
{
  public Guid Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public bool IsActive { get; set; } = true;
  public string? DeactivationReason { get; set; }
  public ulong Version { get; set; }
}
