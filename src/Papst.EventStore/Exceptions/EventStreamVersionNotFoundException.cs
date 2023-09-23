namespace Papst.EventStore.Exceptions;

/// <summary>
/// Thrown if an expected version of a stream is not found
/// </summary>
public class EventStreamVersionNotFoundException : EventStreamException
{
  public ulong Version { get; }

  public EventStreamVersionNotFoundException(Guid streamId, ulong version, string message)
      : base(streamId, message)
  {
    Version = version;
  }

  public EventStreamVersionNotFoundException(Guid streamId, ulong version, string message, Exception innerException)
      : base(streamId, message, innerException)
  {
    Version = version;
  }

  public EventStreamVersionNotFoundException() { }

  public EventStreamVersionNotFoundException(string message) : base(message) { }

  public EventStreamVersionNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}
