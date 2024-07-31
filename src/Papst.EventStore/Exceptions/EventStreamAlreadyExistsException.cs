namespace Papst.EventStore.Exceptions;

/// <summary>
/// Exception that is thrown when the Stream that has been tried to be created already exists
/// </summary>
public class EventStreamAlreadyExistsException : EventStreamException
{
  public EventStreamAlreadyExistsException(Guid streamId, string message)
      : base(streamId, message)
  { }

  public EventStreamAlreadyExistsException(Guid streamId, string message, Exception innerException)
      : base(streamId, message, innerException)
  { }

  public EventStreamAlreadyExistsException() { }

  public EventStreamAlreadyExistsException(string message) : base(message) { }

  public EventStreamAlreadyExistsException(string message, Exception innerException) : base(message, innerException) { }
}
