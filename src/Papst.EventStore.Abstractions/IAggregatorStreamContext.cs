using System;

namespace Papst.EventStore.Abstractions
{
    /// <summary>
    /// Event Stream Context passed onto the Aggregator
    /// </summary>
    public interface IAggregatorStreamContext
    {
        /// <summary>
        /// Id of the Stream
        /// </summary>
        Guid StreamId { get; }

        /// <summary>
        /// The Target Version
        /// </summary>
        ulong TargetVersion { get; }
    }
}
