using System;
using System.Collections.Generic;
using System.Text;

namespace Papst.EventStore.Abstractions.Exceptions
{
    public class EventStreamException : Exception
    {
        public Guid StreamId { get; set; } = Guid.Empty;

        public EventStreamException(Guid streamId, string message) : base(message)
        {
            StreamId = streamId;
        }

        public EventStreamException(Guid streamId, string message, Exception innerException) : base(message, innerException)
        {
            StreamId = streamId;
        }

        public EventStreamException()
        {
        }

        public EventStreamException(string message) : base(message)
        {
        }

        public EventStreamException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
