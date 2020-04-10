namespace Papst.EventStore.Abstractions
{
    /// <summary>
    /// Interface that ensures an Event is Applyable to a Target Type
    /// Allows the <see cref="IEventStore"/> to apply all events to a given Targettype
    /// </summary>
    /// <typeparam name="TTargetType"></typeparam>
    public interface IApplyableEvent<TTargetType>
        where TTargetType: class, new()
    {
        void Apply(TTargetType eventInstance);
    }
}
