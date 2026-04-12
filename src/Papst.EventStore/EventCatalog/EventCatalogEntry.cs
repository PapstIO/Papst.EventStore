namespace Papst.EventStore.EventCatalog;

/// <summary>
/// Lightweight description of an Event registered in the catalog
/// </summary>
/// <param name="EventName">The name of the Event</param>
/// <param name="Description">Optional description of the Event</param>
/// <param name="Constraints">Optional constraints associated with the Event</param>
/// <param name="SchemaJson">The Json Schema of the Event as string</param>
public record EventCatalogEntry(
  string EventName, 
  string? Description, 
  string[]? Constraints,
  Lazy<string> SchemaJson);
