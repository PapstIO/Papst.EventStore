using System.Linq;

namespace Papst.EventStore.Exceptions;

/// <summary>
/// Exception that is thrown when an event catalog lookup by event name matches more than one registration.
/// </summary>
public class EventCatalogAmbiguousEventException : Exception
{
  public EventCatalogAmbiguousEventException(string eventName, IEnumerable<Type> matchingEntityTypes)
    : this(eventName, matchingEntityTypes, null)
  { }

  public EventCatalogAmbiguousEventException(string eventName, int matchCount)
    : this(eventName, null, matchCount)
  { }

  public EventCatalogAmbiguousEventException(string eventName, IEnumerable<Type>? matchingEntityTypes, int? matchCount = null)
    : base(CreateMessage(eventName, matchingEntityTypes, matchCount))
  {
    EventName = eventName;
    MatchingEntityTypes = matchingEntityTypes?.Distinct().ToArray() ?? Array.Empty<Type>();
    MatchCount = matchCount ?? MatchingEntityTypes.Count;
  }

  /// <summary>
  /// The ambiguous event name.
  /// </summary>
  public string EventName { get; }

  /// <summary>
  /// The entity types that matched the event name, when known.
  /// </summary>
  public IReadOnlyList<Type> MatchingEntityTypes { get; }

  /// <summary>
  /// The number of matches that made the lookup ambiguous.
  /// </summary>
  public int MatchCount { get; }

  private static string CreateMessage(string eventName, IEnumerable<Type>? matchingEntityTypes, int? matchCount)
  {
    Type[] entityTypes = matchingEntityTypes?.Distinct().ToArray() ?? Array.Empty<Type>();
    int resolvedMatchCount = matchCount ?? entityTypes.Length;

    if (entityTypes.Length == 0)
    {
      return $"Event catalog lookup for '{eventName}' is ambiguous because {resolvedMatchCount} matching registrations were found.";
    }

    string typeNames = string.Join(", ", entityTypes.Select(t => t.FullName ?? t.Name));
    return $"Event catalog lookup for '{eventName}' is ambiguous because it matches {resolvedMatchCount} registrations for entity types: {typeNames}.";
  }
}
