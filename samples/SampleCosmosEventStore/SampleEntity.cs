using Papst.EventStore.Abstractions;
using System;
using System.Collections.Generic;

namespace SampleCosmosEventStore
{
    internal class SampleEntity : IEntity
    {
        public Guid Id { get; set; }
        public ulong Version { get; set; }
        public string Name { get; set; }
        public Dictionary<string, object> Foo { get; set; }
        public List<string> Associated { get; set; }
    }
}
