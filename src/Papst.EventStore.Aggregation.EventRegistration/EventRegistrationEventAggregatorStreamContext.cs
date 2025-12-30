using System;
using System.Collections.Generic;

namespace Papst.EventStore.Aggregation.EventRegistration;

/// <inheritdoc/>
internal record EventRegistrationEventAggregatorStreamContext(
  Guid StreamId,
  ulong TargetVersion,
  ulong CurrentVersion,
  DateTimeOffset StreamCreated,
  DateTimeOffset EventTime,
  Dictionary<string, string> AggregationData)
  : IAggregatorStreamContext;
