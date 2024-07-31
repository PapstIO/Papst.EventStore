using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;

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

  public EventStreamDocumentMetaDataEntity MetaData { get; init; } = new();
}

public class EventStreamDocumentMetaDataEntity
{
  public string? UserId { get; init; }
  public string? UserName { get; init; }
  public string? TenantId { get; init; }
  public string? Comment { get; init; }
  public Dictionary<string, string>? Additional { get; init; }
}
