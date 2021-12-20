using Papst.EventStore.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SampleCosmosEventStore.Events;

public class SampleCreatedEvent
{
  public Guid EventId { get; set; }
  public string Name { get; set; }
  public Dictionary<string, object> Foo { get; set; }

}

internal class SampleCreatedEventAggregagor : EventAggregatorBase<SampleEntity, SampleCreatedEvent>
{
  public override Task<SampleEntity> ApplyAsync(SampleCreatedEvent evt, SampleEntity entity, IAggregatorStreamContext ctx)
  {

    return Task.FromResult(entity);
  }
}
