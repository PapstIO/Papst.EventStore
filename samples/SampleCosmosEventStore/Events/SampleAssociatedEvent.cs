using Papst.EventStore.Abstractions;
using System.Collections.Generic;

namespace SampleCosmosEventStore.Events
{
    internal class SampleAssociatedEvent : IAggregatableEvent<SampleEntity>
    {
        public string Name { get; set; }

        public void Apply(SampleEntity eventInstance)
        {
            (eventInstance.Associated ??= new List<string>()).Add(Name);
        }
    }
}
