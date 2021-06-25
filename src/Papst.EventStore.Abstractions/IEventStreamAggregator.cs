using System.Threading.Tasks;

namespace Papst.EventStore.Abstractions
{
    /// <summary>
    /// IEventStream Aggregator, Applies all Event of a Stream to a Target Entity
    /// </summary>
    /// <typeparam name="TTargetType"></typeparam>
    public interface IEventStreamAggregator<TTargetType>
        where TTargetType : class, new()
    {
        /// <summary>
        /// Apply the Stream to a new Entity
        /// </summary>
        /// <param name="stream">The Stream</param>
        /// <returns></returns>
        Task<TTargetType> AggregateAsync(IEventStream stream);

        /// <summary>
        /// Apply the Stream to a new Entity, stop at specific version
        /// </summary>
        /// <param name="stream">The Stream</param>
        /// <param name="targetVersion">The target version</param>
        /// <returns></returns>
        Task<TTargetType> AggregateAsync(IEventStream stream, ulong targetVersion);

        /// <summary>
        /// Apply the Stream to an existing entity
        /// </summary>
        /// <param name="stream">The Stream</param>
        /// <param name="target">The Target Entity Instance</param>
        /// <returns></returns>
        Task<TTargetType> AggregateAsync(IEventStream stream, TTargetType target);

        /// <summary>
        /// Apply the Stream to an existing entity, stop at specific version
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="target"></param>
        /// <param name="targetVersion">The target version</param>
        /// <returns></returns>
        Task<TTargetType> AggregateAsync(IEventStream stream, TTargetType target, ulong targetVersion);
    }
}
