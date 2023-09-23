using System;

namespace Papst.EventStore.Abstractions.EventAggregation.EventRegistration;

/// <inheritdoc/>
internal record EventRegistrationEventAggregatorStreamContext(
  Guid StreamId,
  ulong TargetVersion,
  ulong CurrentVersion,
  DateTimeOffset StreamCreated,
  DateTimeOffset EventTime)
  : IAggregatorStreamContext;
