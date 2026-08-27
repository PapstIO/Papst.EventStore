using Papst.EventStore;
using Papst.EventStore.Documents;

namespace Papst.EventStore.Signing.Tests;

/// <summary>
/// Minimal <see cref="IEventStream"/> that exposes a fixed set of documents, for
/// verifier tests that do not need a real store.
/// </summary>
internal sealed class SingleDocumentStream : IEventStream
{
  private readonly IReadOnlyList<EventStreamDocument> _documents;

  public SingleDocumentStream(params EventStreamDocument[] documents) => _documents = documents;

  public Guid StreamId => _documents.Count > 0 ? _documents[0].StreamId : Guid.Empty;
  public ulong Version => _documents.Count == 0 ? 0 : _documents[^1].Version;
  public DateTimeOffset Created => DateTimeOffset.UnixEpoch;
  public ulong? LatestSnapshotVersion => null;
  public EventStreamMetaData MetaData { get; } = new();

  public Task<EventStreamDocument?> GetLatestSnapshot(CancellationToken cancellationToken = default)
    => Task.FromResult<EventStreamDocument?>(null);

  public Task AppendAsync<TEvent>(Guid id, TEvent evt, EventStreamMetaData? metaData = null, CancellationToken cancellationToken = default)
    where TEvent : notnull => throw new NotSupportedException();

  public Task AppendSnapshotAsync<TEntity>(Guid id, TEntity entity, EventStreamMetaData? metaData = null, CancellationToken cancellationToken = default)
    where TEntity : notnull => throw new NotSupportedException();

  public Task<IEventStoreTransactionAppender> CreateTransactionalBatchAsync() => throw new NotSupportedException();

  public async IAsyncEnumerable<EventStreamDocument> ListAsync(ulong startVersion = 0,
    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    foreach (var doc in _documents)
    {
      if (doc.Version >= startVersion)
      {
        yield return doc;
      }
    }

    await Task.CompletedTask;
  }

  public IAsyncEnumerable<EventStreamDocument> ListAsync(ulong startVersion, ulong endVersion, CancellationToken cancellationToken = default)
    => ListAsync(startVersion, cancellationToken);

  public IAsyncEnumerable<EventStreamDocument> ListDescendingAsync(ulong endVersion, ulong startVersion, CancellationToken cancellationToken = default)
    => throw new NotSupportedException();

  public IAsyncEnumerable<EventStreamDocument> ListDescendingAsync(ulong endVersion, CancellationToken cancellationToken = default)
    => throw new NotSupportedException();

  public Task UpdateStreamMetaData(EventStreamMetaData metaData, CancellationToken cancellationToken = default)
    => throw new NotSupportedException();
}
