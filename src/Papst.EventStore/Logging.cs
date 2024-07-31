using Microsoft.Extensions.Logging;

namespace Papst.EventStore;
internal static partial class Logging
{
  [LoggerMessage(EventId = 100_010, EventName = nameof(EventNameResolved), Level = LogLevel.Debug, Message = "Resolved Event {EventName} to {EventType}")]
  public static partial void EventNameResolved(ILogger logger, string eventName, Type eventType);

  [LoggerMessage(EventId = 100_011, EventName = nameof(EventNameNotResolved), Level = LogLevel.Error, Message = "Event {EventName} could not be resolved, please make sure your Events are properly marked with the EventNameAttribute!")]
  public static partial void EventNameNotResolved(ILogger logger, string eventName);

  [LoggerMessage(EventId = 100_012, EventName = nameof(EventTypeResolved), Level = LogLevel.Debug, Message = "Resolved EventType for {EventName} to {EventType}")]
  public static partial void EventTypeResolved(ILogger logger, string eventName, Type eventType);

  [LoggerMessage(EventId = 100_013, EventName = nameof(EventTypeNotResolved), Level = LogLevel.Error, Message = "Event Type {EventType} could not be resolved, please make sure your Events are properly marked with the EventNameAttribute!")]
  public static partial void EventTypeNotResolved(ILogger logger, Type eventType);
}
