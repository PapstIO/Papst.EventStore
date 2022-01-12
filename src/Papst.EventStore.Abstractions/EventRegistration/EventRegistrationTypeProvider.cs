using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Papst.EventStore.Abstractions.EventRegistration;

internal class EventRegistrationTypeProvider : IEventTypeProvider
{

  private readonly ILogger<EventRegistrationTypeProvider> _logger;
  private readonly IEnumerable<IEventRegistration> _eventRegistrations;

  public EventRegistrationTypeProvider(ILogger<EventRegistrationTypeProvider> logger, IEnumerable<IEventRegistration> eventRegistrations)
  {
    _logger = logger;
    _eventRegistrations = eventRegistrations;
  }

  public Type ResolveIdentifier(string dataType)
  {
    var registration = _eventRegistrations.SelectMany(x => x.ReadEvents).FirstOrDefault(x => x.Key == dataType);
    if (registration.Key == null || registration.Value == null)
    {
      Logger.EventNameNotResolved(_logger, dataType);
      throw new NotSupportedException($"Event {dataType} could not be resolved, please make sure your Events are properly marked with the EventNameAttribute!");
    }
    else
    {
      Logger.EventNameResolved(_logger, dataType, registration.Value);
      return registration.Value;
    }
  }

  public string ResolveType(Type type)
  {
    var registration = _eventRegistrations.SelectMany(x => x.WriteEvents).FirstOrDefault(x => x.Key == type);
    if (registration.Key == null || registration.Value == null)
    {
      Logger.EventTypeNotResolved(_logger, type);
      throw new NotSupportedException($"Event Type {type} could not be resolved, please make sure your Events are properly marked with the EventNameAttribute!");
    }
    else
    {
      Logger.EventTypeResolved(_logger, registration.Value, type);
      return registration.Value;
    }
  }

}

public static partial class Logger
{
  private const string EventName = "EventRegistrationTypeProvider";

  [LoggerMessage(EventId = 100_010, EventName = EventName, Level = LogLevel.Debug, Message = "Resolved Event {EventName} to {EventType}")]
  public static partial void EventNameResolved(ILogger logger, string eventName, Type eventType);

  [LoggerMessage(EventId = 100_011, EventName = EventName, Level = LogLevel.Error, Message = "Event {EventName} could not be resolved, please make sure your Events are properly marked with the EventNameAttribute!")]
  public static partial void EventNameNotResolved(ILogger logger, string eventName);

  [LoggerMessage(EventId = 100_012, EventName = EventName, Level = LogLevel.Debug, Message = "Resolved EventType for {EventName} to {EventType}")]
  public static partial void EventTypeResolved(ILogger logger, string eventName, Type eventType);

  [LoggerMessage(EventId = 100_013, EventName = EventName, Level = LogLevel.Error, Message = "Event Type {EventType} could not be resolved, please make sure your Events are properly marked with the EventNameAttribute!")]
  public static partial void EventTypeNotResolved(ILogger logger, Type eventType);
}
