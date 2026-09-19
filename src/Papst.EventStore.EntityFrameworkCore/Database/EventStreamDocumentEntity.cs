using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;
using Papst.EventStore.Documents;

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

  /// <summary>
  /// Cryptographic signature of the document, or <see langword="null"/> when the
  /// stream is not signed. Stored as owned JSON.
  /// </summary>
  public EventSignature? Signature { get; init; }
}

public class EventStreamDocumentMetaDataEntity
{
  public string? UserId { get; init; }
  public string? UserName { get; init; }
  public string? TenantId { get; init; }
  public string? Comment { get; init; }
  public Dictionary<string, string>? Additional { get; init; }
}
