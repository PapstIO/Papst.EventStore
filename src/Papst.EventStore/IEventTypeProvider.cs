namespace Papst.EventStore;

/// <summary>
/// The IEventTypeProvider allows to resolve Event Types to their Names and Names to the Types
/// </summary>
public interface IEventTypeProvider
{
  /// <summary>
  /// Resolve an EventType by its Name
  /// </summary>
  /// <param name="dataType"></param>
  /// <returns></returns>
  Type ResolveIdentifier(string dataType);

  /// <summary>
  /// Resolve an EventType to its Name
  /// </summary>
  /// <param name="type"></param>
  /// <returns></returns>
  string ResolveType(Type type);
}
