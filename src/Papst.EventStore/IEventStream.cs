using Papst.EventStore.Documents;

namespace Papst.EventStore;

/// <summary>
/// Event Stream
/// </summary>
public interface IEventStream
{
  /// <summary>
  /// The Event Stream Id
  /// </summary>
  Guid StreamId { get; }

  /// <summary>
  /// The current Version of the Stream
  /// </summary>
  ulong Version { get; }

  /// <summary>
  /// Time when the Stream has been created
  /// </summary>
  DateTimeOffset Created { get; }
  
  /// <summary>
  /// If not null, contains the Version of the latest snapshop
  /// </summary>
  ulong? LatestSnapshotVersion { get; }

  /// <summary>
  /// The Latest Snapshop that has been fetched (if available)
  /// </summary>
  Task<EventStreamDocument?> GetLatestSnapshot(CancellationToken cancellationToken = default);

  /// <summary>
  /// Appends an Event to the Stream
  /// </summary>
  /// <typeparam name="TEvent">Type of the Event</typeparam>
  /// <param name="id">Id of the Event</param>
  /// <param name="evt">The Event</param>
  /// <param name="metaData">The Events Meta Data</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  Task AppendAsync<TEvent>(
    Guid id,
    TEvent evt,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default
  )
    where TEvent : notnull;
  
  /// <summary>
  /// Appends a new Snapshot to the Stream
  /// </summary>
  /// <param name="id">Id of the Snapshot</param>
  /// <param name="entity">The Entity</param>
  /// <param name="metaData">The Snapshots Meta Data</param>
  /// <param name="cancellationToken"></param>
  /// <typeparam name="TEntity">The Entity Type</typeparam>
  /// <returns></returns>
  Task AppendSnapshotAsync<TEntity>(
    Guid id,
    TEntity entity,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default
  )
    where TEntity : notnull;

  /// <summary>
  /// Create a Batch Appender that allows to append multiple events as a batch for performance reasons
  /// </summary>
  /// <returns></returns>
  Task<IEventStoreTransactionAppender> CreateTransactionalBatchAsync();

  /// <summary>
  /// The Stream Documents
  /// </summary>
  IAsyncEnumerable<EventStreamDocument> ListAsync(ulong startVersion = 0u, CancellationToken cancellationToken = default);

  IAsyncEnumerable<EventStreamDocument> ListAsync(ulong startVersion, ulong endVersion, CancellationToken cancellationToken = default);
}

public interface IEventStoreTransactionAppender
{
  /// <summary>
  /// Appends an Event to the Stream as part of a batch
  /// </summary>
  /// <typeparam name="TEvent">Type of the Event</typeparam>
  /// <param name="id">Id of the Event</param>
  /// <param name="evt">The Event</param>
  /// <param name="metaData">The Events Meta Data</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  IEventStoreTransactionAppender Add<TEvent>(Guid id, TEvent evt, EventStreamMetaData? metaData = null, CancellationToken cancellationToken = default)
    where TEvent: notnull;

  /// <summary>
  /// Commits all Events in the Transactional Appender
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  Task CommitAsync(CancellationToken cancellationToken = default);
}
