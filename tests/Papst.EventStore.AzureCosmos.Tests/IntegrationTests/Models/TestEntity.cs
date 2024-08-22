namespace Papst.EventStore.AzureCosmos.Tests.IntegrationTests.Models;
public class TestEntity : IEntity
{
  public Guid Id { get; set; }
  public ulong Version { get; set; }
}
