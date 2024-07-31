namespace Papst.EventStore.EventRegistration;

/// <summary>
/// Description of an EventSourcing Event
/// </summary>
/// <param name="Name"></param>
/// <param name="IsWrite"></param>
public record EventAttributeDescriptor(string Name, bool IsWrite);
