using System;

namespace Papst.EventStore.Abstractions.EventAggregation.DependencyInjection;


[Serializable]
public class EventTypeNotFoundException : Exception
{
  public EventTypeNotFoundException() { }
  public EventTypeNotFoundException(string message) : base(message) { }
  public EventTypeNotFoundException(string message, Exception inner) : base(message, inner) { }
  protected EventTypeNotFoundException(
  System.Runtime.Serialization.SerializationInfo info,
  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
