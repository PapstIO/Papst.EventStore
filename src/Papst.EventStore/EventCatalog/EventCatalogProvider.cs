using System.Linq;

namespace Papst.EventStore.EventCatalog;

/// <summary>
/// Default implementation of <see cref="IEventCatalog"/> that delegates to registered <see cref="IEventCatalogRegistration"/> instances
/// </summary>
public sealed class EventCatalogProvider : IEventCatalog
{
  private readonly IEnumerable<IEventCatalogRegistration> _registrations;

  public EventCatalogProvider(IEnumerable<IEventCatalogRegistration> registrations)
  {
    _registrations = registrations;
  }

  /// <inheritdoc/>
  public IReadOnlyList<EventCatalogEntry> ListEvents<TEntity>(string? name = null, string[]? constraints = null)
  {
    Type entityType = typeof(TEntity);

    return _registrations
      .SelectMany(r => r.GetEntries(entityType, name, constraints))
      .ToList()
      .AsReadOnly();
  }

  /// <inheritdoc/>
  public EventCatalogEventDetails? GetEventDetails(string eventName)
  {
    return _registrations
      .Select(r => r.GetDetails(eventName))
      .FirstOrDefault(d => d is not null);
  }
}
