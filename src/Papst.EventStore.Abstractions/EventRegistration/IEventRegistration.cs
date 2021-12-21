namespace Papst.EventStore.Abstractions.EventRegistration;

public interface IEventRegistration
{
  IEventRegistration AddEvent<TEven>(EventAttributeDescriptor descriptor);
}
