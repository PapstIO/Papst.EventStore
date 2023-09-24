using System;

namespace Papst.EventStore.Aggregation.EventRegistration;

/// <summary>
/// Marks an Event Sourcing Event
/// If <see cref="IsWriteName"/> is true, the <see cref="Name"/> Property is used to write Events
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class EventNameAttribute : Attribute
{
  /// <summary>
  /// Name of the Event
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// Flag whether the Attribute is used for Writing the Name
  /// </summary>
  public bool IsWriteName { get; }

  /// <summary>
  /// Mark the Event with Read/Write information
  /// </summary>
  /// <param name="name"></param>
  /// <param name="isWriteName"></param>
  public EventNameAttribute(string name, bool isWriteName)
  {
    Name = name;
    IsWriteName = isWriteName;
  }
  
  /// <summary>
  /// Mark the Event with the <paramref name="name"/> as Write Attribute
  /// </summary>
  /// <param name="name"></param>
  public EventNameAttribute(string name) : this(name, true)
  { }
}
