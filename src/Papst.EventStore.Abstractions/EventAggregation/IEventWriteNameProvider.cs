namespace Papst.EventStore.Abstractions.EventAggregation;

public interface IEventWriteNameProvider
{
  string GetEventName<TEvent>();
}
