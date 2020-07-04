namespace Papst.EventStore.Abstractions
{
    /// <summary>
    /// Interface that ensures an Event is Applyable to a Target Type
    /// Allows the <see cref="IEventStore"/> to apply all events to a given Targettype
    /// </summary>
    /// <typeparam name="TTargetType"></typeparam>
    public interface IAggregatableEvent<TTargetType>
        where TTargetType: class, new()
    {
        /// <summary>
        /// Apply the Event to the Target Instance
        /// </summary>
        /// <param name="evt"></param>
        void Apply(TTargetType evt);
    }
}
