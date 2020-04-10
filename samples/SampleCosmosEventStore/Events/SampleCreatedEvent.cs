using System;
using System.Collections.Generic;
using System.Text;

namespace SampleCosmosEventStore.Events
{
    class SampleCreatedEvent
    {
        public Guid EventId { get; set; }
        public string Name { get; set; }
        public Dictionary<string, object> Foo { get; set; }
    }
}
