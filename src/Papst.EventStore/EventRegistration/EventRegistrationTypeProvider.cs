using Microsoft.Extensions.Logging;
using System.Linq;

namespace Papst.EventStore.EventRegistration;
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
      Logging.EventNameNotResolved(_logger, dataType);
      throw new NotSupportedException($"Event {dataType} could not be resolved, please make sure your Events are properly marked with the EventNameAttribute!");
    }
    else
    {
      Logging.EventNameResolved(_logger, dataType, registration.Value);
      return registration.Value;
    }
  }

  public string ResolveType(Type type)
  {
    var registration = _eventRegistrations.SelectMany(x => x.WriteEvents).FirstOrDefault(x => x.Key == type);
    if (registration.Key == null || registration.Value == null)
    {
      Logging.EventTypeNotResolved(_logger, type);
      throw new NotSupportedException($"Event Type {type} could not be resolved, please make sure your Events are properly marked with the EventNameAttribute!");
    }
    else
    {
      Logging.EventTypeResolved(_logger, registration.Value, type);
      return registration.Value;
    }
  }
}
