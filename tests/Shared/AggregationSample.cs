#nullable enable
using System.Collections.Generic;
using Papst.EventStore;
using Papst.EventStore.Aggregation.EventRegistration;

namespace Papst.EventStore.Testing.Aggregation;

/// <summary>
/// Shared sample aggregate + code-generated-aggregation events, link-included into each store test project so
/// the generated <c>AddCodeGeneratedEvents</c> can be exercised against a real store implementation.
/// </summary>
public class SampleOrder : IEntity
{
  public ulong Version { get; set; }
  public string? Customer { get; set; }
  public List<SampleLine> Lines { get; set; } = new();
}

public class SampleLine
{
  public string? Sku { get; set; }
  public int Quantity { get; set; }
}

[EventName(nameof(SampleOrderCreated))]
[EventAggregation<SampleOrder>]
public record SampleOrderCreated(string? Customer);

[EventName(nameof(SampleLineUpserted))]
[EventAggregation<SampleOrder>(PropertyPath = nameof(SampleOrder.Lines))]
public record SampleLineUpserted([property: AggregationCollectionKey("Sku")] string Sku, int Quantity);
