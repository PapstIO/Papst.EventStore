using System;

namespace Papst.EventStore.Abstractions.EventAggregation.EventRegistration;

/// <inheritdoc/>
internal record EventRegistrationEventAggregatorStreamContext : IAggregatorStreamContext
{
  /// <inheritdoc/>
  public Guid StreamId { get; init; }

  /// <inheritdoc/>
  public ulong TargetVersion { get; init; }

  /// <inheritdoc/>
  public ulong CurrentVersion { get; init; }

  /// <inheritdoc/>
  public DateTimeOffset StreamCreated { get; init; }

  /// <inheritdoc/>
  public DateTimeOffset EventTime { get; init; }
}
