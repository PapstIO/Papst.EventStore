using System;

namespace Papst.EventStore.Abstractions;

/// <summary>
/// Marks an Event Sourcing Event
/// If <see cref="IsWriteAttribute"/> is true, the <see cref="Name"/> Property is used to write Events
/// </summary>
//[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
//public sealed class EventSourcingEventAttribute : Attribute
//{
//  public EventSourcingEventAttribute(string name, bool writeAttribute = true)
//  {
//    Name = name;
//    IsWriteAttribute = writeAttribute;
//  }

//  public string Name { get; }
//  public bool IsWriteAttribute { get; }
//}
[System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class EventSourcingEventAttribute : Attribute
{
    /// <summary>
    /// Name of the Event
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Flag whether the Attribute is used for Writing the Name
    /// </summary>
    public bool IsWriteName { get; set; } = true;
}
