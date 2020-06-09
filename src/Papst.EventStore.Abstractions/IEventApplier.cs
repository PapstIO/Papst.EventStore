using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Papst.EventStore.Abstractions
{
    /// <summary>
    /// Event Apply Logic for a specific Event and a Specific Entity
    /// </summary>
    /// <typeparam name="TEntity">The Entity the Event shall be applied</typeparam>
    /// <typeparam name="TEvent">The Event that shall be applied</typeparam>
    public interface IEventApplier<TEntity, TEvent> : IEventApplier<TEntity>
        where TEntity: class
    {
        Task ApplyAsync(TEvent eventInstance, TEntity entityInstance);
    }

    public interface IEventApplier<TEntity>
        where TEntity: class
    {
        Task ApplyAsync(JObject eventInstance, TEntity entityInstance);
    }
}
