using System;

namespace Papst.EventStore.Abstractions.Exceptions;

/// <summary>
/// The exception is thrown when a stream is tried to read but does not exist
/// </summary>
public class EventStreamNotFoundException : EventStreamException
{
  public EventStreamNotFoundException(Guid streamId, string message)
      : base(streamId, message)
  { }

  public EventStreamNotFoundException(Guid streamId, string message, Exception innerException)
      : base(streamId, message, innerException)
  { }

  public EventStreamNotFoundException() { }

  public EventStreamNotFoundException(string message) : base(message) { }

  public EventStreamNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}
