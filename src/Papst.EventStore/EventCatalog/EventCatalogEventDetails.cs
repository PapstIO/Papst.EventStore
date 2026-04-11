namespace Papst.EventStore.EventCatalog;

/// <summary>
/// Detailed description of an Event including its JSON Schema
/// </summary>
/// <param name="EventName">The name of the Event</param>
/// <param name="Description">Optional description of the Event</param>
/// <param name="Constraints">Optional constraints associated with the Event</param>
/// <param name="JsonSchema">JSON Schema of the Event payload</param>
public record EventCatalogEventDetails(string EventName, string? Description, string[]? Constraints, string JsonSchema);
