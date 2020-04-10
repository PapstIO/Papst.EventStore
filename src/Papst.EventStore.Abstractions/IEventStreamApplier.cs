namespace Papst.EventStore.Abstractions
{
    /// <summary>
    /// IEventStream Applier, Applies all Event of a Stream to a Target Entity
    /// </summary>
    /// <typeparam name="TTargetType"></typeparam>
    public interface IEventStreamApplier<TTargetType>
        where TTargetType: class, new()
    {
        /// <summary>
        /// Apply the Stream to a new Entity
        /// </summary>
        /// <param name="stream">The Stream</param>
        /// <returns></returns>
        TTargetType Apply(IEventStream stream);

        /// <summary>
        /// Apply the Stream to an existing entity
        /// </summary>
        /// <param name="stream">The Stream</param>
        /// <param name="target">The Target Entity Instance</param>
        /// <returns></returns>
        TTargetType Apply(IEventStream stream, TTargetType target);
    }
}
