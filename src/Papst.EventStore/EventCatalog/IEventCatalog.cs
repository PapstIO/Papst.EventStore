namespace Papst.EventStore.EventCatalog;

/// <summary>
/// Provides read access to the materialized Event Catalog entries registered in the application.
/// </summary>
public interface IEventCatalog
{
  /// <summary>
  /// List catalog entries registered for entity <typeparamref name="TEntity"/>, optionally filtered by name and/or constraints.
  /// </summary>
  /// <typeparam name="TEntity">The Entity to list events for</typeparam>
  /// <param name="name">Optional event name filter (exact match)</param>
  /// <param name="constraints">Optional constraints filter (entries matching any of the given constraints)</param>
  /// <returns>A list of matching <see cref="EventCatalogEntry"/> instances</returns>
  ValueTask<IReadOnlyList<EventCatalogEntry>> ListEvents<TEntity>(string? name = null, string[]? constraints = null);

  /// <summary>
  /// Get detailed information (including JSON Schema) for a specific event by name.
  /// Throws when the same event name exists for multiple registrations.
  /// </summary>
  /// <param name="eventName">The event name to look up</param>
  /// <returns>The <see cref="EventCatalogEventDetails"/> or <c>null</c> if the event is not registered</returns>
  /// <exception cref="Exceptions.EventCatalogAmbiguousEventException">Thrown when the event name matches more than one registration.</exception>
  ValueTask<EventCatalogEventDetails?> GetEventDetails(string eventName);

  /// <summary>
  /// Get detailed information (including JSON Schema) for a specific event by name, scoped to entity <typeparamref name="TEntity"/>.
  /// Use this overload when the same event name may exist for different entities.
  /// </summary>
  /// <typeparam name="TEntity">The entity type to scope the lookup to</typeparam>
  /// <param name="eventName">The event name to look up</param>
  /// <returns>The <see cref="EventCatalogEventDetails"/> or <c>null</c> if the event is not registered for this entity</returns>
  ValueTask<EventCatalogEventDetails?> GetEventDetails<TEntity>(string eventName);
}
