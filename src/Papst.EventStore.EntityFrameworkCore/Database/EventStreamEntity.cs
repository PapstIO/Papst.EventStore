namespace Papst.EventStore.EntityFrameworkCore.Database;

public class EventStreamEntity
{
  public Guid StreamId { get; init; }
  public DateTimeOffset Created { get; init; }
  public ulong Version { get; set; }
  public ulong NextVersion { get; set; }
  public DateTimeOffset Updated { get; set; }
  public string TargetType { get; init; } = string.Empty;
  public ulong?  LatestSnapshotVersion { get; set; }
}
