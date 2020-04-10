using System;

namespace Papst.EventStore.Abstractions
{
    /// <summary>
    /// Result of a stream append
    /// </summary>
    public class EventStoreResult
    {
        /// <summary>
        /// Id of the Appended document
        /// </summary>
        public Guid? DocumentId { get; set; }

        /// <summary>
        /// Id of the Stream
        /// </summary>
        public Guid StreamId { get; set; }

        /// <summary>
        /// Whether the operation succeeded or not
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// The new Version of the Stream
        /// </summary>
        public ulong Version { get; set; }
    }
}