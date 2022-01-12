namespace Papst.EventStore.Abstractions.EventAggregation.DependencyInjection;

internal class DependencyInjectEventNameProvider : IEventWriteNameProvider
{
  public string GetEventName<TEvent>() => TypeUtils.NameOfType(typeof(TEvent));
}
