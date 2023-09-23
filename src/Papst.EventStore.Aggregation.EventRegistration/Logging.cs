using Microsoft.Extensions.Logging;
using System;

namespace Papst.EventStore.Aggregation.EventRegistration;

public partial class Logging
{
  private const string EventName = "Papst.EventStore";

  [LoggerMessage(EventId = 100_000, EventName = EventName, Level = LogLevel.Debug, Message = "Creating new Entity for Entity {Type} and Stream {StreamId}")]
  public static partial void CreatingNewEntity(ILogger logger, string type, Guid streamId);

  [LoggerMessage(EventId = 100_001, EventName = EventName, Level = LogLevel.Debug, Message = "Applying {EventName} to {Entity} with Id {StreamId}")]
  public static partial void ApplyingEvent(ILogger logger, string eventName, string entity, Guid streamId);

  [LoggerMessage(EventId = 100_002, EventName = EventName, Level = LogLevel.Information, Message = "Entity in Stream {StreamId} has been deleted by Event {EventName} at Version {Version}")]
  public static partial void EntityDeleted(ILogger logger, Guid streamId, string eventName, ulong version);

  [LoggerMessage(EventId = 100_003, EventName = EventName, Level = LogLevel.Error, Message = "Event Aggregator not registered in Dependency Injection for Entity {EntityType} and Event {EventName}")]
  public static partial void EventAggregatorNotRegistered(ILogger logger, Exception exc, string entityType, string eventName);
}
