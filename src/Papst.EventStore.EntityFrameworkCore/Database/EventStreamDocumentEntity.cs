namespace Papst.EventStore.EntityFrameworkCore.Database;

public class EventStreamDocumentEntity
{
  public Guid Id { get; init; }
  public Guid StreamId { get; init; }
  public EventStreamDocumentEntityType Type { get; init; }
  public ulong Version { get; init; }
  public DateTimeOffset Time { get; init; }
  public string Name { get; init; } = string.Empty;
  public string Data { get; init; } = string.Empty;
  public string DataType { get; init; } = string.Empty;
  public string TargetType { get; init; } = string.Empty;

  public string? MetaDataUserId { get; init; }
  public string? MetaDataUserName { get; init; }
  public string? MetaDataTenantId { get; init; }
  public string? MetaDataComment { get; init; }
  public string MetaDataAdditional { get; init; } = "{}";
}
