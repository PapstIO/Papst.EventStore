using System;

namespace Papst.EventStore.Aggregation.EventRegistration;

/// <summary>
/// Maps an Event property to a differently named property on the target entity during code-generated
/// aggregation. Without this attribute the Event property is mapped onto the equally named target property.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class AggregationPropertyAttribute : Attribute
{
  /// <summary>
  /// Name of the property on the target entity (or the object addressed by
  /// <see cref="EventAggregationAttribute{TEntity}.PropertyPath"/>) that this Event property is written to.
  /// </summary>
  public string TargetPropertyName { get; }

  /// <summary>
  /// Maps the property to the given target property name.
  /// </summary>
  /// <param name="targetPropertyName">The target entity property to write to.</param>
  public AggregationPropertyAttribute(string targetPropertyName) => TargetPropertyName = targetPropertyName;
}
