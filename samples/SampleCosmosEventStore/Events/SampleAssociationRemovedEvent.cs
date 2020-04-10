using Papst.EventStore.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampleCosmosEventStore.Events
{
    class SampleAssociationRemovedEvent : IApplyableEvent<SampleEntity>
    {
        public string Name { get; set; }

        public void Apply(SampleEntity eventInstance)
        {
            eventInstance.Associated.Remove(Name);
        }
    }
}
