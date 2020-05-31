using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Papst.EventStore.Abstractions
{
    /// <summary>
    /// Base Implementation for <see cref="IEventApplier{TEntity, TEvent}"/> that provides
    /// basic mapping between <see cref="JObject"/> and <see cref="TEvent"/>
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TEvent"></typeparam>
    public abstract class BaseEventApplier<TEntity, TEvent> : IEventApplier<TEntity, TEvent>
    {
        /// <inheritdoc/>
        public abstract Task ApplyAsync(TEvent eventInstance, TEntity entityInstance);

        /// <summary>
        /// Calls <see cref="ApplyAsync(TEvent, TEntity)"/> by converting the <see cref="JObject"/> to <see cref="TEvent"/>
        /// </summary>
        /// <param name="eventInstance"></param>
        /// <param name="entityInstance"></param>
        /// <returns></returns>
        public Task ApplyAsync(JObject eventInstance, TEntity entityInstance)
            => ApplyAsync(eventInstance.ToObject<TEvent>(), entityInstance);
    }
}
