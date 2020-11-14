using Papst.EventStore.Abstractions;
using System.Threading.Tasks;

namespace SampleCosmosEventStore.Events
{
    public class SampleAssociationRemovedEvent
    {
        public string Name { get; set; }
    }

    internal class SampleAssociationRemovedEventAggregatore : EventAggregatorBase<SampleEntity, SampleAssociationRemovedEvent>
    {
        public override Task<SampleEntity> ApplyAsync(SampleAssociationRemovedEvent evt, SampleEntity entity, IAggregatorStreamContext ctx)
        {
            entity.Associated.Remove(evt.Name);

            return Task.FromResult(entity);
        }
    }
}
