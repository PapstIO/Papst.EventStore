namespace Papst.EventStore.Abstractions.EventRegistration;

/// <summary>
/// 
/// </summary>
public interface IEventRegistration
{
  void AddEvent<TEvent>(params EventAttributeDescriptor[] descriptors);
}
