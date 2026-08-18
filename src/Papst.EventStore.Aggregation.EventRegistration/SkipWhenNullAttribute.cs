using System;

namespace Papst.EventStore.Aggregation.EventRegistration;

/// <summary>
/// Overrides the <see cref="EventAggregationAttribute{TEntity}.SkipNullValues"/> setting for a single Event
/// property during code-generated aggregation.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class SkipWhenNullAttribute : Attribute
{
  /// <summary>
  /// When <see langword="true"/> the property is only written onto the target when its Event value is not
  /// <see langword="null"/>. When <see langword="false"/> the property is always written, even when
  /// <see langword="null"/>.
  /// </summary>
  public bool SkipWhenNull { get; }

  /// <summary>
  /// Marks the property with an explicit skip-when-null behaviour, overriding the Event's global setting.
  /// </summary>
  /// <param name="skipWhenNull">Whether <see langword="null"/> values shall be skipped for this property.</param>
  public SkipWhenNullAttribute(bool skipWhenNull) => SkipWhenNull = skipWhenNull;
}
