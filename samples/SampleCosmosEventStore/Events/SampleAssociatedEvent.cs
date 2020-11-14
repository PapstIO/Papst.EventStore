using Papst.EventStore.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SampleCosmosEventStore.Events
{
    public class SampleAssociatedEvent
    {
        public string Name { get; set; }
    }

    internal class SampleAssociatedEventAggregator : EventAggregatorBase<SampleEntity, SampleAssociatedEvent>
    {
        public override Task<SampleEntity> ApplyAsync(SampleAssociatedEvent evt, SampleEntity entity, IAggregatorStreamContext ctx)
        {
            (entity.Associated ??= new List<string>()).Add(evt.Name);
            return Task.FromResult(entity);
        }
    }
}
