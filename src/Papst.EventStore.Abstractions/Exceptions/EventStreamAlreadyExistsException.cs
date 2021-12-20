using System;

namespace Papst.EventStore.Abstractions.Exceptions;

/// <summary>
/// Exception that is thrown when the Stream that has been tried to be created already exists
/// </summary>
public class EventStreamAlreadyExistsException : EventStreamException
{
  public EventStreamAlreadyExistsException(Guid streamid, string message)
      : base(streamid, message)
  { }

  public EventStreamAlreadyExistsException(Guid streamid, string message, Exception innerException)
      : base(streamid, message, innerException)
  { }

  public EventStreamAlreadyExistsException() { }

  public EventStreamAlreadyExistsException(string message) : base(message) { }

  public EventStreamAlreadyExistsException(string message, Exception innerException) : base(message, innerException) { }
}
