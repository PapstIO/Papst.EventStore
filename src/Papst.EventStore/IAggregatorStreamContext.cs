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
  /// Sets the aggregation data associated with the specified key and version.
  /// </summary>
  /// <remarks>If aggregation data already exists for the specified key and version, it will be overwritten. Use
  /// the validUntilVersion parameter to specify a range of versions for which the data is applicable.</remarks>
  /// <param name="key">The unique identifier for the aggregation data to set. Cannot be null or empty.</param>
  /// <param name="version">The version number to associate with the aggregation data.</param>
  /// <param name="value">The value to store for the specified key and version. Cannot be null.</param>
  /// <param name="validUntilVersion">An optional version number indicating the last version for which the aggregation data remains valid. If null, the
  /// data is considered valid indefinitely.</param>
  void SetAggregationData(string key, ulong version, string value, ulong? validUntilVersion = null);

  /// <summary>
  /// Retrieves the aggregation context data associated with the specified key.
  /// </summary>
  /// <param name="key">The unique identifier for the aggregation context data to retrieve. Cannot be null.</param>
  /// <param name="ignoreValidity">If set to <see langword="true"/>, the method will return the data regardless of its version validity constraints.</param>
  /// <returns>An <see cref="AggregationContextData"/> instance if data is found for the specified key; otherwise, <see
  /// langword="null"/>. Returns <see langword="null"/>, if the specified context data is no longer valid due to the version constraint.</returns>
  AggregationContextData? GetAggregationData(string key, bool ignoreValidity = false);
}
