using System.Linq;

namespace Papst.EventStore.EventCatalog;

/// <summary>
/// In-memory implementation of <see cref="IEventCatalogRegistration"/>
/// </summary>
public sealed class EventCatalogRegistration : IEventCatalogRegistration
{
  private readonly record struct CatalogItem(string EventName, string? Description, string[]? Constraints, Lazy<string> SchemaJson);

  private readonly Dictionary<Type, List<CatalogItem>> _entityEvents = new();
  private readonly Dictionary<string, CatalogItem> _eventsByName = new();

  /// <inheritdoc/>
  public void RegisterEvent<TEntity>(string eventName, string? description, string[]? constraints, Lazy<string> schemaJson)
  {
    Type entityType = typeof(TEntity);
    CatalogItem item = new(eventName, description, constraints, schemaJson);

    if (!_entityEvents.TryGetValue(entityType, out List<CatalogItem>? items))
    {
      items = new List<CatalogItem>();
      _entityEvents[entityType] = items;
    }
    items.Add(item);

    _eventsByName.TryAdd(eventName, item);
  }

  /// <inheritdoc/>
  IReadOnlyList<EventCatalogEntry> IEventCatalogRegistration.GetEntries(Type entityType, string? name, string[]? constraints)
  {
    if (!_entityEvents.TryGetValue(entityType, out List<CatalogItem>? items))
    {
      return Array.Empty<EventCatalogEntry>();
    }

    IEnumerable<CatalogItem> filtered = items;

    if (name is not null)
    {
      filtered = filtered.Where(i => i.EventName == name);
    }

    if (constraints is { Length: > 0 })
    {
      filtered = filtered.Where(i =>
        i.Constraints is not null &&
        i.Constraints.Any(c => constraints.Contains(c)));
    }

    return filtered
      .Select(i => new EventCatalogEntry(i.EventName, i.Description, i.Constraints))
      .ToList()
      .AsReadOnly();
  }

  /// <inheritdoc/>
  EventCatalogEventDetails? IEventCatalogRegistration.GetDetails(string eventName)
  {
    if (!_eventsByName.TryGetValue(eventName, out CatalogItem item))
    {
      return null;
    }

    return new EventCatalogEventDetails(item.EventName, item.Description, item.Constraints, item.SchemaJson.Value);
  }
}
