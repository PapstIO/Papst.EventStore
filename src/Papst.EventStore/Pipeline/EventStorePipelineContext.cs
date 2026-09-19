namespace Papst.EventStore.Pipeline;

/// <summary>
/// Base class for the strongly typed context that flows through an Event Store
/// pipeline. Handlers read from and mutate the concrete context; results live on
/// the context rather than being returned, mirroring the ASP.NET Core middleware
/// pipeline.
/// </summary>
public abstract class EventStorePipelineContext
{
  /// <summary>
  /// The Stream the pipeline operation targets.
  /// </summary>
  public required Guid StreamId { get; init; }

  /// <summary>
  /// Per-operation state bag for passing data between handlers and the terminal,
  /// in the style of <c>HttpContext.Items</c>.
  /// </summary>
  public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();
}
