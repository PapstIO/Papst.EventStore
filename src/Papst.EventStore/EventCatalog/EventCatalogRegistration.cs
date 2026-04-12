using System.Linq;

namespace Papst.EventStore.EventCatalog;

/// <summary>
/// In-memory implementation of <see cref="IEventCatalogRegistration"/>
/// </summary>
public sealed class EventCatalogRegistration : IEventCatalogRegistration
{
  private readonly Dictionary<Type, List<EventCatalogEntry>> _entityEvents = new();

  /// <inheritdoc/>
  public void RegisterEvent<TEntity>(string eventName, string? description, string[]? constraints, Lazy<string> schemaJson)
  {
    Type entityType = typeof(TEntity);
    EventCatalogEntry item = new(eventName, description, constraints, schemaJson);

    if (!_entityEvents.TryGetValue(entityType, out var items))
    {
      items = [];
      _entityEvents[entityType] = items;
    }
    items.Add(item);
  }

  public IEnumerable<KeyValuePair<Type, IReadOnlyCollection<EventCatalogEntry>>> GetEntries()
  {
    return _entityEvents.Select(x => new KeyValuePair<Type, IReadOnlyCollection<EventCatalogEntry>>(x.Key, x.Value.ToArray()));
  }
}
