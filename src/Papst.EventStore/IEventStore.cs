namespace Papst.EventStore;

public interface IEventStore
{
  /// <summary>
  /// Reads a <see cref="IEventStream"/> Async
  /// </summary>
  /// <param name="streamId">Id of the Stream</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <exception cref="Exceptions.EventStreamNotFoundException"></exception>
  /// <exception cref="Exceptions.EventStreamVersionMismatchException"></exception>
  Task<IEventStream> GetAsync(
    Guid streamId,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Creates a new empty <see cref="IEventStream"/> Async
  /// </summary>
  /// <param name="streamId">The stream Id</param>
  /// <param name="targetTypeName">The Name of the Target Type</param>
  /// <param name="cancellationToken"></param>
  /// <exception cref="Exceptions.EventStreamAlreadyExistsException">Thrown when the StreamId is already taken</exception>
  /// <returns></returns>
  Task<IEventStream> CreateAsync(
    Guid streamId,
    string targetTypeName,
    CancellationToken cancellationToken = default);
}
