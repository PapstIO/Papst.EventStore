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
  /// Get catalog entries for a given entity type, optionally filtered
  /// </summary>
  internal IReadOnlyList<EventCatalogEntry> GetEntries(Type entityType, string? name, string[]? constraints);

  /// <summary>
  /// Get detailed event information by name
  /// </summary>
  internal EventCatalogEventDetails? GetDetails(string eventName);
}
