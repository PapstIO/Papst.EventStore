using Papst.EventStore.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace Papst.EventStore.CosmosDb
{
    /// <inheritdoc/>
    class CosmosEventStream : IEventStream
    {
        private List<EventStreamDocument> _documents;

        /// <inheritdoc/>
        public EventStreamDocument LatestSnapShot => _documents?.Where(doc => doc.DocumentType == EventStreamDocumentType.Snapshot).LastOrDefault();

        /// <inheritdoc/>
        public IReadOnlyList<EventStreamDocument> Stream => _documents;

        public CosmosEventStream(IEnumerable<EventStreamDocument> documents)
        {
            _documents = documents as List<EventStreamDocument> ?? documents.ToList();
        }
    }
}
