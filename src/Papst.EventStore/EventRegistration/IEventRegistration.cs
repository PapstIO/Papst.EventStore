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

public class EventDescriptionEventRegistration : IEventRegistration
{
  private readonly Dictionary<string, Type> _readEvents = new();
  private readonly Dictionary<Type, string> _writeEvents = new();

  IReadOnlyDictionary<string, Type> IEventRegistration.ReadEvents => _readEvents;

  IReadOnlyDictionary<Type, string> IEventRegistration.WriteEvents => _writeEvents;

  public void AddEvent<TEvent>(params EventAttributeDescriptor[] descriptors)
  {
    foreach (var descriptor in descriptors)
    {
      if (descriptor.IsWrite)
      {
        _writeEvents.Add(typeof(TEvent), descriptor.Name);
      }

      _readEvents.Add(descriptor.Name, typeof(TEvent));
    }
  }

  void IEventRegistration.AddEvent<TEvent>(params EventAttributeDescriptor[] descriptors) => AddEvent<TEvent>(descriptors);
}
