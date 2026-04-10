namespace Papst.EventStore.EventCatalog;

/// <summary>
/// Provides read access to the Event Catalog, allowing queries for registered events per entity
/// </summary>
public interface IEventCatalog
{
  /// <summary>
  /// List events registered for entity <typeparamref name="TEntity"/>, optionally filtered by name and/or constraints
  /// </summary>
  /// <typeparam name="TEntity">The Entity to list events for</typeparam>
  /// <param name="name">Optional event name filter (exact match)</param>
  /// <param name="constraints">Optional constraints filter (events matching any of the given constraints)</param>
  /// <returns>A list of matching <see cref="EventCatalogEntry"/> instances</returns>
  IReadOnlyList<EventCatalogEntry> ListEvents<TEntity>(string? name = null, string[]? constraints = null);

  /// <summary>
  /// Get detailed information (including JSON Schema) for a specific event by name
  /// </summary>
  /// <param name="eventName">The event name to look up</param>
  /// <returns>The <see cref="EventCatalogEventDetails"/> or <c>null</c> if the event is not registered</returns>
  EventCatalogEventDetails? GetEventDetails(string eventName);
}
