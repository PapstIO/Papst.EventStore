using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace Papst.EventStore.Abstractions
{
    /// <summary>
    /// Base Implementation for <see cref="IEventAggregator{TEntity, TEvent}"/> that provides
    /// basic mapping between <see cref="JObject"/> and <see cref="TEvent"/>
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TEvent"></typeparam>
    public abstract class EventAggregatorBase<TEntity, TEvent> : IEventAggregator<TEntity, TEvent>
        where TEntity: class
    {
        /// <inheritdoc/>
        public abstract Task<TEntity?> ApplyAsync(TEvent evt, TEntity entity, IAggregatorStreamContext ctx);

        /// <summary>
        /// Calls <see cref="ApplyAsync(TEvent, TEntity)"/> by converting the <see cref="JObject"/> to <see cref="TEvent"/>
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="entity"></param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public Task<TEntity?> ApplyAsync(JObject evt, TEntity entity, IAggregatorStreamContext ctx)
            => ApplyAsync(evt.ToObject<TEvent>()!, entity, ctx);


        /// <summary>
        /// Executes the <paramref name="setter"/> action when <paramref name="value"/> is not null
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="setter"></param>
        /// <param name="value"></param>
        protected void SetIfNotNull<TProperty>(Action<TProperty> setter, TProperty? value)
        {
            if (value != null)
            {
                setter(value);
            }
        }
    }
}
