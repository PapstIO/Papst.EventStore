using System;

namespace Papst.EventStore.Abstractions.Exceptions
{
    public class EventStreamVersionMismatchException : EventStreamException
    {
        public EventStreamVersionMismatchException(Guid streamId, string message) 
            : base(streamId, message)
        {
        }

        public EventStreamVersionMismatchException(Guid streamId, string message, Exception innerException) 
            : base(streamId, message, innerException)
        {
        }

        public EventStreamVersionMismatchException()
        {
        }

        public EventStreamVersionMismatchException(string message) : base(message)
        {
        }

        public EventStreamVersionMismatchException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
