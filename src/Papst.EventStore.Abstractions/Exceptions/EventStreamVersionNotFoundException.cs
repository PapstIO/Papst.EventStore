using System;
using System.Collections.Generic;
using System.Text;

namespace Papst.EventStore.Abstractions.Exceptions
{
    public class EventStreamVersionNotFoundException : EventStreamException
    {
        public EventStreamVersionNotFoundException(Guid streamId, string message) 
            : base(streamId, message)
        {
        }

        public EventStreamVersionNotFoundException(Guid streamId, string message, Exception innerException) 
            : base(streamId, message, innerException)
        {
        }

        public EventStreamVersionNotFoundException()
        {
        }

        public EventStreamVersionNotFoundException(string message) : base(message)
        {
        }

        public EventStreamVersionNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
