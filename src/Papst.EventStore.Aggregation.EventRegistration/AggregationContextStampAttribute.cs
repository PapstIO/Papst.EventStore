using System;

namespace Papst.EventStore.Aggregation.EventRegistration;

/// <summary>
/// Marks an <b>Entity</b> property to be stamped from the stream context during aggregation, independent of any
/// Event property. The <c>Papst.EventStore.CodeGeneration</c> source generator emits the assignment into every
/// generated aggregator for the Entity.
/// When <see cref="OnEveryEvent"/> is <see langword="true"/> (the default) the property is written on every applied
/// Event (e.g. an <c>Updated</c> timestamp); when <see langword="false"/> it is written only when the Entity is
/// newly created (<c>ctx.CurrentVersion == 0</c>), e.g. <c>Id</c> / <c>Created</c>.
/// </summary>
/// <remarks>
/// The stamp is decoupled from the Event's properties, so it needs no matching Event field. If the selected
/// <see cref="AggregationContextValue"/> is not implicitly convertible to the target property's type the generator
/// reports <c>EVTSRC0005</c> and skips the stamp.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class AggregationContextStampAttribute : Attribute
{
  /// <summary>The stream-context value to stamp onto the property.</summary>
  public AggregationContextValue Value { get; }

  /// <summary>
  /// When <see langword="true"/> (default) the property is stamped on every applied Event; when
  /// <see langword="false"/> it is stamped only when the Entity is newly created (<c>ctx.CurrentVersion == 0</c>).
  /// </summary>
  public bool OnEveryEvent { get; init; } = true;

  /// <summary>Marks the property to be stamped with the given stream-context <paramref name="value"/>.</summary>
  /// <param name="value">The stream-context value to write onto the property.</param>
  public AggregationContextStampAttribute(AggregationContextValue value) => Value = value;
}
