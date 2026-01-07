using Papst.EventStore.Documents;

namespace Papst.EventStore;

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
