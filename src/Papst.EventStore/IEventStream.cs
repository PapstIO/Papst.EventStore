using Papst.EventStore.Documents;
using System.Linq;

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
  /// Allows to store Metadata that is consistent over the complete stream
  /// </summary>
  EventStreamMetaData MetaData { get; }

  /// <summary>
  /// The Latest Snapshot that has been fetched (if available)
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
  /// List all Stream Documents ascending starting with <paramref name="startVersion"/>
  /// </summary>
  /// <param name="startVersion"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  IAsyncEnumerable<EventStreamDocument> ListAsync(ulong startVersion = 0u, CancellationToken cancellationToken = default);

  /// <summary>
  /// List the Stream Documents ascending from <paramref name="startVersion"/> to <paramref name="endVersion"/>
  /// </summary>
  /// <param name="startVersion"></param>
  /// <param name="endVersion"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  IAsyncEnumerable<EventStreamDocument> ListAsync(ulong startVersion, ulong endVersion, CancellationToken cancellationToken = default);
  
  /// <summary>
  /// List the Stream Documents descending from <paramref name="endVersion"/> to <paramref name="startVersion"/>
  /// </summary>
  /// <param name="endVersion"></param>
  /// <param name="startVersion"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  IAsyncEnumerable<EventStreamDocument> ListDescendingAsync(ulong endVersion, ulong startVersion, CancellationToken cancellationToken = default);
  
  /// <summary>
  /// List the Stream documents from <paramref name="endVersion"/> to 0
  /// </summary>
  /// <param name="endVersion"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  IAsyncEnumerable<EventStreamDocument> ListDescendingAsync(ulong endVersion, CancellationToken cancellationToken = default);

  /// <summary>
  /// List the Stream Documents ascending from <paramref name="startVersion"/> to <paramref name="endVersion"/>
  /// whose <see cref="EventStreamDocument.Name"/> is one of <paramref name="eventNames"/>.
  /// The names are matched exactly; when an event has several registered names, pass every name it has been written with.
  /// The default implementation filters while enumerating, stores can override it to filter in the database.
  /// </summary>
  /// <param name="startVersion"></param>
  /// <param name="endVersion"></param>
  /// <param name="eventNames">Names of the Documents to return, an empty collection returns no Documents</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  IAsyncEnumerable<EventStreamDocument> ListAsync(ulong startVersion, ulong endVersion, IReadOnlyCollection<string> eventNames, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(eventNames);
    return ListAsync(startVersion, endVersion, cancellationToken).Where(doc => eventNames.Contains(doc.Name));
  }

  /// <summary>
  /// List the Stream Documents descending from <paramref name="endVersion"/> to <paramref name="startVersion"/>
  /// whose <see cref="EventStreamDocument.Name"/> is one of <paramref name="eventNames"/>.
  /// The names are matched exactly; when an event has several registered names, pass every name it has been written with.
  /// The default implementation filters while enumerating, stores can override it to filter in the database.
  /// </summary>
  /// <param name="endVersion"></param>
  /// <param name="startVersion"></param>
  /// <param name="eventNames">Names of the Documents to return, an empty collection returns no Documents</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  IAsyncEnumerable<EventStreamDocument> ListDescendingAsync(ulong endVersion, ulong startVersion, IReadOnlyCollection<string> eventNames, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(eventNames);
    return ListDescendingAsync(endVersion, startVersion, cancellationToken).Where(doc => eventNames.Contains(doc.Name));
  }
  
  /// <summary>
  /// Updates the Even Stream Metadata
  /// </summary>
  /// <param name="metaData"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  Task UpdateStreamMetaData(EventStreamMetaData metaData, CancellationToken cancellationToken = default);
}
