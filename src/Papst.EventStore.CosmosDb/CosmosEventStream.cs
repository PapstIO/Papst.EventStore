using Papst.EventStore.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Papst.EventStore.CosmosDb
{
    /// <inheritdoc/>
    class CosmosEventStream : IEventStream
    {
        private List<EventStreamDocument> _documents;

        public Guid StreamId { get; }

        /// <inheritdoc/>
        public EventStreamDocument LatestSnapShot => _documents?.Where(doc => doc.DocumentType == EventStreamDocumentType.Snapshot).LastOrDefault();

        /// <inheritdoc/>
        public IReadOnlyList<EventStreamDocument> Stream => _documents;

        public CosmosEventStream(Guid streamId, IEnumerable<EventStreamDocument> documents)
        {
            StreamId = streamId;
            _documents = documents as List<EventStreamDocument> ?? documents.ToList();
        }
    }
}
