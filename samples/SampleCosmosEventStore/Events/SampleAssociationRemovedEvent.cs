using Papst.EventStore.Abstractions;

namespace SampleCosmosEventStore.Events
{
    class SampleAssociationRemovedEvent : IAggregatableEvent<SampleEntity>
    {
        public string Name { get; set; }

        public void Apply(SampleEntity eventInstance)
        {
            eventInstance.Associated.Remove(Name);
        }
    }
}
