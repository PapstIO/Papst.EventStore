using System;

namespace Papst.EventStore.Abstractions
{
    public class EventStoreResult
    {
        public Guid? DocumentId { get; set; }
        public Guid StreamId { get; set; }
        public bool IsSuccess { get; set; }
        public ulong Version { get; set; }
    }
}