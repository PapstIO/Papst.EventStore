using System;
using System.Collections.Generic;

namespace Papst.EventStore.Abstractions.EventRegistration;

public class EventRegistration : IEventRegistration
{
  private readonly Dictionary<string, Type> _readEvents = new();
  private readonly Dictionary<Type, string> _writeEvents = new();

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
}
