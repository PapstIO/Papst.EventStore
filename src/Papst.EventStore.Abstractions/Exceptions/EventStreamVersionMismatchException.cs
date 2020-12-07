using System;

namespace Papst.EventStore.Abstractions.Exceptions
{
    /// <summary>
    /// Exception that is thrown if the expected version of a stream does not match
    /// </summary>
    public class EventStreamVersionMismatchException : EventStreamException
    {
        /// <summary>
        /// The Expected Version of the stream
        /// </summary>
        public ulong ExpectedVersion { get; set; }

        /// <summary>
        /// The current version of the stream
        /// </summary>
        public ulong CurrentVersion { get; set; }

        public EventStreamVersionMismatchException(Guid streamId, ulong expected, ulong actual, string message)
            : base(streamId, message)
        {
            ExpectedVersion = expected;
            CurrentVersion = actual;
        }

        public EventStreamVersionMismatchException(Guid streamId, ulong expected, ulong actual, string message, Exception innerException)
            : base(streamId, message, innerException)
        {
            ExpectedVersion = expected;
            CurrentVersion = actual;
        }

        public EventStreamVersionMismatchException() { }

        public EventStreamVersionMismatchException(string message) : base(message) { }

        public EventStreamVersionMismatchException(string message, Exception innerException) : base(message, innerException) { }

        public EventStreamVersionMismatchException(Guid streamId, string message) : base(streamId, message) { }

        public EventStreamVersionMismatchException(Guid streamId, string message, Exception innerException) : base(streamId, message, innerException) { }
    }
}
