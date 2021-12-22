using System;

namespace Papst.EventStore.Abstractions.EventRegistration;

public interface IEventTypeProvider
{
  Type ResolveIdentifier(string dataType);

  string ResolveType(Type type);
}
