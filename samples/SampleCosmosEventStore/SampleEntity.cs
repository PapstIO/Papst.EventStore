using System;
using System.Collections.Generic;
using System.Text;

namespace SampleCosmosEventStore
{
    class SampleEntity
    {
        public Guid EventId { get; set; }
        public string Name { get; set; }
        public Dictionary<string, object> Foo { get; set; }

        public List<string> Associated { get; set; }

        //public SampleEntity()
        //{

        //}
    }
}
