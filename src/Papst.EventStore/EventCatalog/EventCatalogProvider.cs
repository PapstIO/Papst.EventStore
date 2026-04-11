using System.Linq;
using Papst.EventStore.Exceptions;

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
  public ValueTask<IReadOnlyList<EventCatalogEntry>> ListEvents<TEntity>(string? name = null, string[]? constraints = null)
  {
    Type entityType = typeof(TEntity);

    IReadOnlyList<EventCatalogEntry> result = _registrations
      .SelectMany(r => r.GetEntries(entityType, name, constraints))
      .ToList()
      .AsReadOnly();

    return new ValueTask<IReadOnlyList<EventCatalogEntry>>(result);
  }

  /// <inheritdoc/>
  public ValueTask<EventCatalogEventDetails?> GetEventDetails(string eventName)
  {
    List<EventCatalogEventDetails> matches = _registrations
      .Select(r => r.GetDetails(eventName))
      .Where(d => d is not null)
      .Cast<EventCatalogEventDetails>()
      .ToList();

    if (matches.Count > 1)
    {
      throw new EventCatalogAmbiguousEventException(eventName, matches.Count);
    }

    return new ValueTask<EventCatalogEventDetails?>(matches.FirstOrDefault());
  }

  /// <inheritdoc/>
  public ValueTask<EventCatalogEventDetails?> GetEventDetails<TEntity>(string eventName)
  {
    Type entityType = typeof(TEntity);

    EventCatalogEventDetails? result = _registrations
      .Select(r => r.GetDetails(entityType, eventName))
      .FirstOrDefault(d => d is not null);

    return new ValueTask<EventCatalogEventDetails?>(result);
  }
}
