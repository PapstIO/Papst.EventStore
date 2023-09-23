namespace Papst.EventStore.EventRegistration;

/// <summary>
/// 
/// </summary>
public interface IEventRegistration
{
  void AddEvent<TEvent>(params EventAttributeDescriptor[] descriptors);

  internal IReadOnlyDictionary<string, Type> ReadEvents { get; }
  internal IReadOnlyDictionary<Type, string> WriteEvents { get; }
}
