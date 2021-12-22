using System;

namespace Papst.EventStore.Abstractions.EventRegistration;

public interface IEventTypeProvider
{
  Type GetEventType(string dataType);

  string GetEventWriteType(Type type);
}
