namespace Papst.EventStore.Abstractions
{
    internal class EventStream : IEventStream
    {
        EventStreamDocument LatestSnapshot { get; }

        
    }
}