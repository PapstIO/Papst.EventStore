namespace Papst.EventStore.Aggregation.EventRegistration;

/// <summary>
/// A stream-context value that an <see cref="AggregationContextStampAttribute"/> writes onto an Entity property
/// during aggregation. The values mirror the members of <c>Papst.EventStore.IAggregatorStreamContext</c>.
/// </summary>
public enum AggregationContextValue
{
  /// <summary>The time the current Event was created (<c>ctx.EventTime</c>).</summary>
  EventTime,

  /// <summary>The id of the Stream (<c>ctx.StreamId</c>).</summary>
  StreamId,

  /// <summary>The time the Stream was created (<c>ctx.StreamCreated</c>).</summary>
  StreamCreated,

  /// <summary>The Entity version before the current Event is aggregated (<c>ctx.CurrentVersion</c>).</summary>
  CurrentVersion,

  /// <summary>The target version of the aggregation (<c>ctx.TargetVersion</c>).</summary>
  TargetVersion,
}
