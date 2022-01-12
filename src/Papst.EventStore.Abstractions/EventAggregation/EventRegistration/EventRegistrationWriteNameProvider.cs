using Papst.EventStore.Abstractions.EventRegistration;

namespace Papst.EventStore.Abstractions.EventAggregation.EventRegistration;

internal class EventRegistrationWriteNameProvider : IEventWriteNameProvider
{
  private readonly IEventTypeProvider _typeProvider;

  public EventRegistrationWriteNameProvider(IEventTypeProvider eventTypeProvider)
  {
    _typeProvider = eventTypeProvider;
  }
  public string GetEventName<TEvent>() => _typeProvider.ResolveType(typeof(TEvent));
}
