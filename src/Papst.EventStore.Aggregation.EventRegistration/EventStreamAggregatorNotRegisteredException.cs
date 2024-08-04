using System;
using Papst.EventStore.Exceptions;

namespace Papst.EventStore.Aggregation.EventRegistration;

public class EventStreamAggregatorNotRegisteredException : EventStreamException
{
  public EventStreamAggregatorNotRegisteredException(
    InvalidOperationException exc,
    Guid streamId,
    string entityType,
    string eventName
  )
    : base(streamId, $"The Event Aggregator for the event {eventName} and EntityType {entityType} is not registered.", exc)
  {
    EntityType = entityType;
    EventName = eventName;
  }

  public string EventName { get; }

  public string EntityType { get; }
}
