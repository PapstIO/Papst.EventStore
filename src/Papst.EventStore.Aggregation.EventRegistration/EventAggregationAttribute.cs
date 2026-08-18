using System;

namespace Papst.EventStore.Aggregation.EventRegistration;

/// <summary>
/// Opt-in marker that declares the complete aggregation instruction for an Event.
/// When an Event Class or Record is decorated with this attribute, the
/// <c>Papst.EventStore.CodeGeneration</c> source generator emits an
/// <see cref="EventAggregatorBase{TEntity,TEvent}"/> implementation that copies the Event's
/// properties onto <typeparamref name="TEntity"/> (or a nested target selected via <see cref="PropertyPath"/>)
/// and registers it in the <c>AddCodeGeneratedEvents</c> DI extension.
/// This runs in parallel with hand-written aggregators; it does not replace them.
/// </summary>
/// <typeparam name="TEntity">The Entity the Event shall be aggregated on</typeparam>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class EventAggregationAttribute<TEntity> : Attribute
  where TEntity : class
{
  /// <summary>
  /// Dot-separated path from <typeparamref name="TEntity"/> to the object that shall receive the Event's
  /// values. An empty string (the default) targets the Entity itself.
  /// When the path resolves to a <see cref="System.Collections.Generic.IDictionary{TKey,TValue}"/> the Event
  /// property marked with <see cref="AggregationDictionaryKeyAttribute"/> selects the entry to update;
  /// when it resolves to a collection the Event property marked with
  /// <see cref="AggregationCollectionKeyAttribute"/> selects the item to update.
  /// </summary>
  public string PropertyPath { get; set; } = string.Empty;

  /// <summary>
  /// Global flag controlling whether <see langword="null"/> Event values are skipped (default <see langword="true"/>)
  /// or written onto the target. Can be overridden per property with <see cref="SkipWhenNullAttribute"/>.
  /// </summary>
  public bool SkipNullValues { get; set; } = true;
}
