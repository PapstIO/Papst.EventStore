using Papst.EventStore.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Papst.EventStore.CosmosDb;

/// <inheritdoc/>
internal class CosmosEventStream : IEventStream
{
  private readonly List<EventStreamDocument> _documents;

  public Guid StreamId { get; }

  /// <inheritdoc/>
  public EventStreamDocument? LatestSnapShot => _documents.LastOrDefault(doc => doc.DocumentType == EventStreamDocumentType.Snapshot);

  /// <inheritdoc/>
  public IReadOnlyList<EventStreamDocument> Stream => _documents;

  public CosmosEventStream(Guid streamId, IEnumerable<EventStreamDocument> documents)
  {
    StreamId = streamId;
    _documents = documents as List<EventStreamDocument> ?? documents.ToList();
  }
}
