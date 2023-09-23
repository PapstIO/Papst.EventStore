using Microsoft.Extensions.Logging;

namespace Papst.EventStore;
internal static partial class Logging
{
  private const string EventName = "Papst.EventStore";

  [LoggerMessage(EventId = 100_010, EventName = EventName, Level = LogLevel.Debug, Message = "Resolved Event {EventName} to {EventType}")]
  public static partial void EventNameResolved(ILogger logger, string eventName, Type eventType);

  [LoggerMessage(EventId = 100_011, EventName = EventName, Level = LogLevel.Error, Message = "Event {EventName} could not be resolved, please make sure your Events are properly marked with the EventNameAttribute!")]
  public static partial void EventNameNotResolved(ILogger logger, string eventName);

  [LoggerMessage(EventId = 100_012, EventName = EventName, Level = LogLevel.Debug, Message = "Resolved EventType for {EventName} to {EventType}")]
  public static partial void EventTypeResolved(ILogger logger, string eventName, Type eventType);

  [LoggerMessage(EventId = 100_013, EventName = EventName, Level = LogLevel.Error, Message = "Event Type {EventType} could not be resolved, please make sure your Events are properly marked with the EventNameAttribute!")]
  public static partial void EventTypeNotResolved(ILogger logger, Type eventType);
}
