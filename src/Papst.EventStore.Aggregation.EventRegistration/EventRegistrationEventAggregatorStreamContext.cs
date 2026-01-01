using System;
using System.Collections.Generic;

namespace Papst.EventStore.Aggregation.EventRegistration;

/// <inheritdoc/>
internal record EventRegistrationEventAggregatorStreamContext(
  Guid StreamId,
  ulong TargetVersion,
  ulong CurrentVersion,
  DateTimeOffset StreamCreated,
  DateTimeOffset EventTime)
  : IAggregatorStreamContext
{
  private readonly Dictionary<string, AggregationContextData> _aggregationData = new();

  /// <inheritdoc/>
  public void SetAggregationData(string key, ulong version, string value, ulong? validUntilVersion = null)
  {
    _aggregationData[key] = new AggregationContextData(
      key,
      version,
      validUntilVersion,
      value);
  }

  /// <inheritdoc/>
  public AggregationContextData? GetAggregationData(string key, bool ignoreValidity = false)
  {
    if (!_aggregationData.TryGetValue(key, out var data))
    {
      return null;
    }
    if (!ignoreValidity && (data.Version > CurrentVersion || data.ValidUntilVersion is not null && CurrentVersion > data.ValidUntilVersion))
    {
      return null;
    }
    return data;
  }
}
