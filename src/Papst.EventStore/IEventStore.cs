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
  /// <param name="streamId">The StreamId</param>
  /// <param name="targetTypeName">The Name of the Target Type</param>
  /// <param name="cancellationToken"></param>
  /// <exception cref="Exceptions.EventStreamAlreadyExistsException">Thrown when the StreamId is already taken</exception>
  /// <returns></returns>
  Task<IEventStream> CreateAsync(
    Guid streamId,
    string targetTypeName,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Creates a new empty <see cref="IEventStream"/> with MetaData
  /// </summary>
  /// <param name="streamId">The Stream Id</param>
  /// <param name="targetTypeName">The name of the Target Type</param>
  /// <param name="tenantId">The tenant id</param>
  /// <param name="userId">the creating user Id</param>
  /// <param name="username">the creating username</param>
  /// <param name="comment">A Stream wide comment</param>
  /// <param name="additionalMetaData">Additional Meta Data</param>
  /// <param name="cancellationToken"></param>
  /// <exception cref="Exceptions.EventStreamAlreadyExistsException">Thrown when the StreamId is already taken</exception>
  /// <returns></returns>
  Task<IEventStream> CreateAsync(
    Guid streamId,
    string targetTypeName,
    string? tenantId,
    string? userId,
    string? username,
    string? comment,
    Dictionary<string, string>? additionalMetaData,
    CancellationToken cancellationToken = default);
}
