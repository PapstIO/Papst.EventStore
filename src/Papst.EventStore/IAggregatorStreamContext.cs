namespace Papst.EventStore;

/// <summary>
/// Event Stream Context passed onto the Aggregator
/// </summary>
public interface IAggregatorStreamContext
{
  /// <summary>
  /// Id of the Stream
  /// </summary>
  Guid StreamId { get; }

  /// <summary>
  /// The Target Version
  /// </summary>
  ulong TargetVersion { get; }

  /// <summary>
  /// Version before the current Event is Aggregated on the Entity
  /// </summary>
  ulong CurrentVersion { get; }

  /// <summary>
  /// Timestamp where the Stream has been created
  /// </summary>
  DateTimeOffset StreamCreated { get; }

  /// <summary>
  /// The Time when the Event was created
  /// </summary>
  DateTimeOffset EventTime { get; }
  
  /// <summary>
  /// Dictonary containing information that is transferred between aggregations
  /// </summary>
  Dictionary<string, string> AggregationData { get; }
}
