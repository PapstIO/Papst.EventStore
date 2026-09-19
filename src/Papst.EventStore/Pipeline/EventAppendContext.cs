using Papst.EventStore.Documents;

namespace Papst.EventStore.Pipeline;

/// <summary>
/// One document being appended. A mutable wrapper so handlers can replace the
/// immutable <see cref="EventStreamDocument"/> record (e.g. to attach a signature).
/// </summary>
public sealed class EventAppendEntry
{
  /// <summary>
  /// The document to be appended. Handlers may replace it, typically via
  /// <c>Document = Document with { ... }</c>.
  /// </summary>
  public EventStreamDocument Document { get; set; }

  /// <summary>
  /// Creates a new entry for the given <paramref name="document"/>.
  /// </summary>
  public EventAppendEntry(EventStreamDocument document) => Document = document;
}

/// <summary>
/// Pipeline context for appending one or more documents to a stream. A single
/// append carries one entry; a batch commit carries several, in version order.
/// </summary>
public sealed class EventAppendContext : EventStorePipelineContext
{
  /// <summary>
  /// The documents being appended, in ascending version order. One entry for a
  /// single append, multiple for a batch commit.
  /// </summary>
  public required IReadOnlyList<EventAppendEntry> Entries { get; init; }

  /// <summary>
  /// The type of the documents being appended (Event or Snapshot).
  /// </summary>
  public required EventStreamDocumentType DocumentType { get; init; }

  /// <summary>
  /// The Meta Data supplied for the append, if any.
  /// </summary>
  public EventStreamMetaData? MetaData { get; init; }

  /// <summary>
  /// The signature <see cref="EventSignature.Value"/> of the stream's current
  /// chain head (the previous document), or <see langword="null"/> when the stream
  /// has no prior signed document. Supplied by the store from its index.
  /// </summary>
  public string? PreviousSignature { get; init; }

  /// <summary>
  /// True when more than one document is being appended in this operation.
  /// </summary>
  public bool IsBatch => Entries.Count > 1;
}
