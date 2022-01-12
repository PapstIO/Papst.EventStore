using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Papst.EventStore.Abstractions;

/// <summary>
/// Event Store
/// </summary>
public interface IEventStore
{
  /// <summary>
  /// Reads a <see cref="EventStream"/> Async
  /// </summary>
  /// <param name="streamId">Id of the Stream</param>
  /// <param name="fromVersion">Starting Version</param>
  /// <param name="token"></param>
  /// <returns></returns>
  /// <exception cref="Exceptions.EventStreamNotFoundException"></exception>
  /// <exception cref="Exceptions.EventStreamVersionMismatchException"></exception>
  Task<IEventStream> ReadAsync(Guid streamId, ulong fromVersion, CancellationToken token = default);

  /// <summary>
  /// Reads a complete <see cref="EventStream"/> Async
  /// </summary>
  /// <param name="streamId"></param>
  /// <param name="token"></param>
  /// <returns></returns>
  /// <exception cref="Exceptions.EventStreamNotFoundException"></exception>
  Task<IEventStream> ReadAsync(Guid streamId, CancellationToken token = default);

  /// <summary>
  /// Reads a <see cref="EventStream"/> from the Latest Snapshot
  /// </summary>
  /// <param name="streamId"></param>
  /// <param name="token"></param>
  /// <returns></returns>
  /// <exception cref="Exceptions.EventStreamNotFoundException"></exception>
  Task<IEventStream> ReadFromSnapshotAsync(Guid streamId, CancellationToken token = default);

  /// <summary>
  /// Appends a <see cref="EventStreamDocument"/> to the <see cref="EventStream"/> Async
  /// </summary>
  /// <param name="streamId"></param>
  /// <param name="expectedVersion"></param>
  /// <param name="doc"></param>
  /// <param name="token"></param>
  /// <returns></returns>
  /// <exception cref="Exceptions.EventStreamNotFoundException"></exception>
  /// <exception cref="Exceptions.EventStreamVersionMismatchException"></exception>
  Task<EventStoreResult> AppendAsync(Guid streamId, ulong expectedVersion, EventStreamDocument doc, CancellationToken token = default);

  /// <summary>
  /// Append multiple <see cref="EventStreamDocument"/> to the <see cref="EventStream"/> Async
  /// </summary>
  /// <param name="streamId"></param>
  /// <param name="expectedVersion"></param>
  /// <param name="documents"></param>
  /// <param name="token"></param>
  /// <returns></returns>
  /// <exception cref="Exceptions.EventStreamNotFoundException"></exception>
  /// <exception cref="Exceptions.EventStreamVersionMismatchException"></exception>
  Task<EventStoreResult> AppendAsync(Guid streamId, ulong expectedVersion, IEnumerable<EventStreamDocument> documents, CancellationToken token = default);

  /// <summary>
  /// Appends a Snapshot to the Stream
  /// </summary>
  /// <param name="streamId"></param>
  /// <param name="expectedVersion"></param>
  /// <param name="snapshot"></param>
  /// <param name="deleteOlderSnapshots"></param>
  /// <param name="token"></param>
  /// <exception cref="Exceptions.EventStreamNotFoundException"></exception>
  /// <exception cref="Exceptions.EventStreamVersionMismatchException"></exception>
  /// <returns></returns>
  Task<EventStoreResult> AppendSnapshotAsync(Guid streamId, ulong expectedVersion, EventStreamDocument snapshot, bool deleteOlderSnapshots = true, CancellationToken token = default);

  /// <summary>
  /// Creates a new <see cref="EventStream"/> Async
  /// </summary>
  /// <param name="streamId"></param>
  /// <param name="doc"></param>
  /// <param name="token"></param>
  /// <exception cref="Exceptions.EventStreamAlreadyExistsException">Thrown when the StreamId is already taken</exception>
  /// <returns></returns>
  Task<IEventStream> CreateAsync(Guid streamId, EventStreamDocument doc, CancellationToken token = default);

  /// <summary>
  /// Append Event to Store
  /// </summary>
  /// <typeparam name="TDocument">Type of the Body</typeparam>
  /// <typeparam name="TTargetType">Type of the Target Read Model</typeparam>
  /// <param name="store">The Store where the Event will be appended</param>
  /// <param name="streamId">The Stream Id</param>
  /// <param name="name">Name of the Event</param>
  /// <param name="expectedVersion">Expected Version of the Stream</param>
  /// <param name="document">The Body</param>
  /// <param name="userId">Id of the User who Appended the Document</param>
  /// <param name="username">Name of the User who appended the Document</param>
  /// <param name="tenantId">Tenant that owns the document</param>
  /// <param name="comment">Comment for the Event</param>
  /// <param name="additional">Additional Meta Items</param>
  /// <param name="token">Cancellation Token for the operation</param>
  /// <returns></returns>
  Task<EventStoreResult> AppendEventAsync<TDocument, TTargetType>(Guid streamId, string name, ulong expectedVersion, TDocument document, Guid? userId = null, string? username = null, Guid? tenantId = null, string? comment = null, Dictionary<string, string>? additional = null, CancellationToken token = default) where TDocument : class;

  /// <summary>
  /// Creates a new Event Stream
  /// </summary>
  /// <typeparam name="TDocument">Type of the Body</typeparam>
  /// <typeparam name="TTargetType">Type of the Target Read Model</typeparam>
  /// <param name="store">The Store where the Event will be appended</param>
  /// <param name="streamId">The Stream Id</param>
  /// <param name="name">Name of the Event</param>
  /// <param name="document">The Body</param>
  /// <param name="userId">Id of the User who Appended the Document</param>
  /// <param name="username">Name of the User who appended the Document</param>
  /// <param name="tenantId">Tenant that owns the document</param>
  /// <param name="comment">Comment for the Event</param>
  /// <param name="additional">Additional Meta Items</param>
  /// <param name="token">Cancellation Token for the operation</param>
  /// <returns></returns>
  Task<IEventStream> CreateEventStreamAsync<TDocument, TTargetType>(Guid streamId, string name, TDocument document, Guid? userId = null, string? username = null, Guid? tenantId = null, string? comment = null, Dictionary<string, string>? additional = null, CancellationToken token = default) where TDocument : class;
}
