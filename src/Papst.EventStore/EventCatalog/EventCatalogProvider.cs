using System.Collections.Frozen;
using System.Linq;
using Papst.EventStore.Exceptions;

namespace Papst.EventStore.EventCatalog;

/// <summary>
/// Default implementation of <see cref="IEventCatalog"/> that materializes registered <see cref="IEventCatalogRegistration"/> entries.
/// </summary>
public sealed class EventCatalogProvider : IEventCatalog
{
  private readonly FrozenDictionary<Type, FrozenSet<EventCatalogEntry>> _registrations;

  public EventCatalogProvider(IEnumerable<IEventCatalogRegistration> registrations)
  {
    _registrations = registrations
      .SelectMany(registration => registration.GetEntries())
      .GroupBy(entry => entry.Key)
      .ToFrozenDictionary(
        group => group.Key,
        group => group
          .SelectMany(entry => entry.Value)
          .ToFrozenSet()
      );
  }

  /// <inheritdoc/>
  public ValueTask<IReadOnlyList<EventCatalogEntry>> ListEvents<TEntity>(string? name = null, string[]? constraints = null)
  {
    Type entityType = typeof(TEntity);

    if (!_registrations.TryGetValue(entityType, out FrozenSet<EventCatalogEntry>? registrationsForEntity))
    {
      return new([]);
    }

    IReadOnlyList<EventCatalogEntry> result = registrationsForEntity
      .Where(entry => MatchesName(entry, name) && MatchesConstraints(entry, constraints))
      .ToArray();

    return new ValueTask<IReadOnlyList<EventCatalogEntry>>(result);
  }

  /// <inheritdoc/>
  public ValueTask<EventCatalogEventDetails?> GetEventDetails(string eventName)
  {
    List<(Type EntityType, EventCatalogEntry Entry)> matches = _registrations
      .SelectMany(pair => pair.Value.Select(entry => (EntityType: pair.Key, Entry: entry)))
      .Where(match => match.Entry.EventName == eventName)
      .ToList();

    if (matches.Count == 0)
    {
      return new ValueTask<EventCatalogEventDetails?>((EventCatalogEventDetails?)null);
    }

    if (matches.Count > 1)
    {
      throw new EventCatalogAmbiguousEventException(eventName, matches.Select(match => match.EntityType), matches.Count);
    }

    return new ValueTask<EventCatalogEventDetails?>(CreateDetails(matches[0].Entry));
  }

  /// <inheritdoc/>
  public ValueTask<EventCatalogEventDetails?> GetEventDetails<TEntity>(string eventName)
  {
    Type entityType = typeof(TEntity);

    if (!_registrations.TryGetValue(entityType, out FrozenSet<EventCatalogEntry>? registrationsForEntity))
    {
      return new ValueTask<EventCatalogEventDetails?>((EventCatalogEventDetails?)null);
    }

    EventCatalogEntry? entry = registrationsForEntity.FirstOrDefault(candidate => candidate.EventName == eventName);

    EventCatalogEventDetails? result = entry is null ? null : CreateDetails(entry);

    return new ValueTask<EventCatalogEventDetails?>(result);
  }

  private static bool MatchesName(EventCatalogEntry entry, string? name)
  {
    return name is null || entry.EventName == name;
  }

  private static bool MatchesConstraints(EventCatalogEntry entry, string[]? constraints)
  {
    if (constraints is not { Length: > 0 })
    {
      return true;
    }

    return entry.Constraints is not null &&
           entry.Constraints.Any(constraints.Contains);
  }

  private static EventCatalogEventDetails CreateDetails(EventCatalogEntry entry)
  {
    return new EventCatalogEventDetails(entry.EventName, entry.Description, entry.Constraints, entry.SchemaJson.Value);
  }
}
