using System;

namespace Papst.EventStore.Abstractions.Exceptions
{
    public class EventStreamNotFoundException : EventStreamException
    {
        public EventStreamNotFoundException(Guid streamId, string message) 
            : base(streamId, message)
        {
        }

        public EventStreamNotFoundException(Guid streamId, string message, Exception innerException) 
            : base(streamId, message, innerException)
        {
        }

        public EventStreamNotFoundException()
        {
        }

        public EventStreamNotFoundException(string message) : base(message)
        {
        }

        public EventStreamNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
