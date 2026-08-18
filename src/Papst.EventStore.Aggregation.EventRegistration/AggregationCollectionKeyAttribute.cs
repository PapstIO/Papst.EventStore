using System;

namespace Papst.EventStore.Aggregation.EventRegistration;

/// <summary>
/// Marks an Event property as the search criteria within the collection located at the
/// <see cref="EventAggregationAttribute{TEntity}.PropertyPath"/>. During aggregation the item whose
/// <see cref="TargetPropertyName"/> equals this Event property's value is updated with the Event's remaining
/// properties; when no matching item exists a new item is created, its key property is set and the item is
/// added to the collection (upsert).
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class AggregationCollectionKeyAttribute : Attribute
{
  /// <summary>
  /// Name of the property on the collection's item type that is compared against this Event property's value.
  /// </summary>
  public string TargetPropertyName { get; }

  /// <summary>
  /// Marks the property as the collection search key.
  /// </summary>
  /// <param name="targetPropertyName">The item property to match against (e.g. <c>"Id"</c>).</param>
  public AggregationCollectionKeyAttribute(string targetPropertyName) => TargetPropertyName = targetPropertyName;
}
