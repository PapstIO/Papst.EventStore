namespace Papst.EventStore;

public interface IEventTypeProvider
{
  Type ResolveIdentifier(string dataType);

  string ResolveType(Type type);
}
