namespace Papst.EventStore.Documents;

/// <summary>
/// Meta Data Object of a Event Stream Document
/// </summary>
public record EventStreamMetaData
{
  /// <summary>
  /// Id of the User that caused the Document
  /// </summary>
  public string? UserId { get; init; }

  /// <summary>
  /// Name of the User that caused the Document
  /// </summary>
  public string? UserName { get; init; }

  /// <summary>
  /// Optional: TenantId
  /// </summary>
  public string? TenantId { get; init; }

  /// <summary>
  /// Comment if available
  /// </summary>
  public string? Comment { get; init; }

  /// <summary>
  /// Additional Meta Data Properties
  /// </summary>
  public Dictionary<string, string>? Additional { get; init; }
}
