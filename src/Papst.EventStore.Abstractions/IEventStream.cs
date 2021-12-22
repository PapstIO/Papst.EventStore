using System;
using System.Collections.Generic;

namespace Papst.EventStore.Abstractions;

/// <summary>
/// Event Stream
/// </summary>
public interface IEventStream
{
  /// <summary>
  /// The Event Stream Id
  /// </summary>
  Guid StreamId { get; }

  /// <summary>
  /// The Latest Snapshop that has been fetched (if available)
  /// </summary>
  EventStreamDocument? LatestSnapShot { get; }

  /// <summary>
  /// The fetched Documents
  /// </summary>
  IReadOnlyList<EventStreamDocument> Stream { get; }
}
