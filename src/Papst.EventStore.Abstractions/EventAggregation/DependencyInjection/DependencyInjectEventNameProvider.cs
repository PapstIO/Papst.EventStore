using Papst.EventStore.Abstractions.EventRegistration;
using System;

namespace Papst.EventStore.Abstractions.EventAggregation.DependencyInjection;

internal class DependencyInjectEventNameProvider : IEventTypeProvider
{
  public Type ResolveIdentifier(string dataType) => TypeUtils.TypeOfName(dataType) ?? throw new EventTypeNotFoundException($"Could not find Event for DataType {dataType}");

  public string ResolveType(Type type) => TypeUtils.NameOfType(type);
}
