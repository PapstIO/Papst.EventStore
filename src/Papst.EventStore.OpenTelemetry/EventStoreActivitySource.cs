using System.Diagnostics;
using System.Reflection;

namespace Papst.EventStore.OpenTelemetry;

/// <summary>
/// Provides the <see cref="ActivitySource"/> for Papst.EventStore OpenTelemetry instrumentation.
/// </summary>
public static class EventStoreActivitySource
{
  /// <summary>
  /// The name of the ActivitySource used for all EventStore activities.
  /// Use this name when configuring OpenTelemetry to listen to EventStore traces.
  /// </summary>
  public const string SourceName = "Papst.EventStore";

  internal static readonly ActivitySource Source = new(
    SourceName,
    Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0");
}
