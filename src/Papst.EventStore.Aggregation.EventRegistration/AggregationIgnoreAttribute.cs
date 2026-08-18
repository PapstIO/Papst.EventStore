using System;

namespace Papst.EventStore.Aggregation.EventRegistration;

/// <summary>
/// Excludes an Event property from code-generated aggregation. The property is never mapped onto the target
/// entity, even when an equally named target property exists.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class AggregationIgnoreAttribute : Attribute
{
}
