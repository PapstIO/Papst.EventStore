using Papst.EventStore.Abstractions;
using System.Collections.Generic;

namespace SampleCosmosEventStore.Events
{
    class SampleAssociatedEvent : IApplyableEvent<SampleEntity>
    {
        public string Name { get; set; }

        public void Apply(SampleEntity eventInstance)
        {
            if (eventInstance.Associated == null)
            {
                eventInstance.Associated = new List<string>();
            }
            eventInstance.Associated.Add(Name);
        }
    }
}
