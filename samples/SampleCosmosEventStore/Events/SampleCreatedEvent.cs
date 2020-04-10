using Papst.EventStore.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampleCosmosEventStore.Events
{
    class SampleCreatedEvent : IApplyableEvent<SampleEntity>
    {
        public Guid EventId { get; set; }
        public string Name { get; set; }
        public Dictionary<string, object> Foo { get; set; }

        public void Apply(SampleEntity eventInstance)
        {
            eventInstance.Name = Name;
            eventInstance.Foo = Foo;
        }
    }
}
