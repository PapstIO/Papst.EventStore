namespace Papst.EventStore.Aggregation.EventRegistration;

/// <summary>
/// Selects how a keyed collection or dictionary target (addressed by
/// <see cref="EventAggregationAttribute{TEntity}.PropertyPath"/>) is mutated by an aggregated Event. The element to
/// act on is located via the Event property marked with <see cref="AggregationCollectionKeyAttribute"/> or
/// <see cref="AggregationDictionaryKeyAttribute"/>. Ignored for single-object targets.
/// </summary>
public enum AggregationMode
{
  /// <summary>Insert or update the keyed element (the default, unchanged behavior).</summary>
  Upsert,

  /// <summary>Remove the keyed element / dictionary entry.</summary>
  RemoveByKey,
}
