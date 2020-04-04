using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Papst.EventStore.Abstractions
{
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
        Task<EventStream> ReadAsync(Guid streamId, ulong fromVersion, CancellationToken token = default);

        /// <summary>
        /// Reads a complete <see cref="EventStream"/> Async
        /// </summary>
        /// <param name="streamId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="Exceptions.EventStreamNotFoundException"></exception>
        Task<EventStream> ReadAsync(Guid streamId, CancellationToken token = default);

        /// <summary>
        /// Reads a <see cref="EventStream"/> from the Latest Snapshot
        /// </summary>
        /// <param name="streamId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="Exceptions.EventStreamNotFoundException"></exception>
        Task<EventStream> ReadFromSnapshotAsync(Guid streamId, CancellationToken token = default);

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
        /// <param name="stringId"></param>
        /// <param name="doc"></param>
        /// <param name="token"></param>
        /// <exception cref="Exceptions.EventStreamAlreadyExistsException">Thrown when the StreamId is already taken</exception>
        /// <returns></returns>
        Task<EventStream> CreateAsync(Guid stringId, EventStreamDocument doc, CancellationToken token = default);

    }
}
