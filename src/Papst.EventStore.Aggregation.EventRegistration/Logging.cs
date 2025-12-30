using Microsoft.Extensions.Logging;
using System;

namespace Papst.EventStore.Aggregation.EventRegistration;

public static partial class Logging
{
  [LoggerMessage(LogLevel.Debug, "Creating new Entity for Entity {Type} and Stream {StreamId}")]
  public static partial void CreatingNewEntity(this ILogger logger, string type, Guid streamId);

  [LoggerMessage(LogLevel.Debug, "Applying {EventName} to {Entity} with Id {StreamId}")]
  public static partial void ApplyingEvent(this ILogger logger, string eventName, string entity, Guid streamId);

  [LoggerMessage(LogLevel.Information, "Entity in Stream {StreamId} has been deleted by Event {EventName} at Version {Version}")]
  public static partial void EntityDeleted(this ILogger logger, Guid streamId, string eventName, ulong version);

  [LoggerMessage(LogLevel.Error, "Event Aggregator not registered in Dependency Injection for Entity {EntityType} and Event {EventName}")]
  public static partial void EventAggregatorNotRegistered(this ILogger logger, Exception exc, string entityType, string eventName);
}
