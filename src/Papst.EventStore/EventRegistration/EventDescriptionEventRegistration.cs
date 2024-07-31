namespace Papst.EventStore.EventRegistration;

/// <summary>
/// In Memory Type Registration
/// </summary>
public sealed class EventDescriptionEventRegistration : IEventRegistration
{
  private readonly Dictionary<string, Type> _readEvents = new();
  private readonly Dictionary<Type, string> _writeEvents = new();

  IReadOnlyDictionary<string, Type> IEventRegistration.ReadEvents => _readEvents;

  IReadOnlyDictionary<Type, string> IEventRegistration.WriteEvents => _writeEvents;

  public void AddEvent<TEvent>(params EventAttributeDescriptor[] descriptors)
  {
    foreach (EventAttributeDescriptor descriptor in descriptors)
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
