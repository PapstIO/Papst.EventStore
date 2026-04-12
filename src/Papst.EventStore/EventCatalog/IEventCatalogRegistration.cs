namespace Papst.EventStore.EventCatalog;

/// <summary>
/// Registration interface used by generated code to populate the Event Catalog
/// </summary>
public interface IEventCatalogRegistration
{
  /// <summary>
  /// Register an event for a specific entity type
  /// </summary>
  /// <typeparam name="TEntity">The entity type this event is associated with</typeparam>
  /// <param name="eventName">The name of the event</param>
  /// <param name="description">Optional description</param>
  /// <param name="constraints">Optional constraints</param>
  /// <param name="schemaJson">Lazily evaluated JSON schema string</param>
  void RegisterEvent<TEntity>(string eventName, string? description, string[]? constraints, Lazy<string> schemaJson);

  /// <summary>
  /// Returns all registered catalog entries grouped by their entity type.
  /// </summary>
  /// <returns>The registered catalog entries keyed by entity type.</returns>
  IEnumerable<KeyValuePair<Type, IReadOnlyCollection<EventCatalogEntry>>> GetEntries();
}
