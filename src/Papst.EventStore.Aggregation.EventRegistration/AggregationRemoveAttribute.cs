using System;

namespace Papst.EventStore.Aggregation.EventRegistration;

/// <summary>
/// Marks an Event property whose value identifies the item(s) to remove from the collection or dictionary
/// located at the <see cref="EventAggregationAttribute{TEntity}.PropertyPath"/>. During aggregation the
/// matching item(s) are removed from the Entity instead of being upserted; the Event's remaining properties
/// are not applied.
/// </summary>
/// <remarks>
/// The property value may be a single key or an <see cref="System.Collections.Generic.IEnumerable{T}"/> of
/// keys (bulk removal). How an item is matched depends on the target selected by
/// <see cref="EventAggregationAttribute{TEntity}.PropertyPath"/>:
/// <list type="bullet">
/// <item><description>
/// Dictionary (<c>IDictionary&lt;TKey, TValue&gt;</c>): the value is the key of the entry to remove.
/// </description></item>
/// <item><description>
/// Collection (<c>ICollection&lt;T&gt;</c>) combined with <see cref="AggregationCollectionKeyAttribute"/>:
/// every item whose key property equals the value is removed.
/// </description></item>
/// <item><description>
/// Collection of scalars (<c>ICollection&lt;T&gt;</c> whose element type matches the value): every element
/// equal to the value is removed.
/// </description></item>
/// </list>
/// Removing a key that is not present is a no-op.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class AggregationRemoveAttribute : Attribute
{
}
