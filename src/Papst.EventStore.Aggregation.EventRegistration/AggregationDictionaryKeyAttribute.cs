using System;

namespace Papst.EventStore.Aggregation.EventRegistration;

/// <summary>
/// Marks an Event property as the key into the dictionary located at the
/// <see cref="EventAggregationAttribute{TEntity}.PropertyPath"/>. During aggregation the entry stored under
/// this key is updated with the Event's remaining properties; when no entry exists a new value is created and
/// inserted (upsert).
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class AggregationDictionaryKeyAttribute : Attribute
{
}
