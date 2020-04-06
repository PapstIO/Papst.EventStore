using System;

namespace Papst.EventStore.Abstractions.Exceptions
{
    public class EventStreamAlreadyExistsException : EventStreamException
    {
        public EventStreamAlreadyExistsException(Guid streamid, string message) 
            : base(streamid, message)
        {
        }

        public EventStreamAlreadyExistsException(Guid streamid, string message, Exception innerException) 
            : base(streamid, message, innerException)
        {
        }

        public EventStreamAlreadyExistsException()
        {
        }

        public EventStreamAlreadyExistsException(string message) : base(message)
        {
        }

        public EventStreamAlreadyExistsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
