using System.Linq;
using Papst.EventStore.Exceptions;

namespace Papst.EventStore.EventCatalog;

/// <summary>
/// In-memory implementation of <see cref="IEventCatalogRegistration"/>
/// </summary>
public sealed class EventCatalogRegistration : IEventCatalogRegistration
{
  private readonly record struct CatalogItem(string EventName, string? Description, string[]? Constraints, Lazy<string> SchemaJson);

  private readonly Dictionary<Type, List<CatalogItem>> _entityEvents = new();

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
  }

  /// <inheritdoc/>
  public IReadOnlyList<EventCatalogEntry> GetEntries(Type entityType, string? name, string[]? constraints)
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
  public EventCatalogEventDetails? GetDetails(string eventName)
  {
    List<(Type EntityType, CatalogItem Item)> matches = _entityEvents
      .SelectMany(pair => pair.Value
        .Where(item => item.EventName == eventName)
        .Select(item => (pair.Key, item)))
      .ToList();

    if (matches.Count == 0)
    {
      return null;
    }

    if (matches.Count > 1)
    {
      throw new EventCatalogAmbiguousEventException(eventName, matches.Select(match => match.EntityType), matches.Count);
    }

    return CreateDetails(matches[0].Item);
  }

  /// <inheritdoc/>
  public EventCatalogEventDetails? GetDetails(Type entityType, string eventName)
  {
    if (!_entityEvents.TryGetValue(entityType, out List<CatalogItem>? items))
    {
      return null;
    }

    CatalogItem? found = items.Cast<CatalogItem?>().FirstOrDefault(i => i!.Value.EventName == eventName);
    if (!found.HasValue)
    {
      return null;
    }

    return CreateDetails(found.Value);
  }

  private static EventCatalogEventDetails CreateDetails(CatalogItem item)
  {
    return new EventCatalogEventDetails(item.EventName, item.Description, item.Constraints, item.SchemaJson.Value);
  }
}
