using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Papst.EventStore.Abstractions;

/// <summary>
/// Event Aggregation Logic for a specific Event and a Specific Entity
/// </summary>
/// <typeparam name="TEntity">The Entity the Event shall be aggregated on</typeparam>
/// <typeparam name="TEvent">The Event that shall be aggregated</typeparam>
public interface IEventAggregator<TEntity, TEvent> : IEventAggregator<TEntity>
    where TEntity : class
{
  Task<TEntity?> ApplyAsync(TEvent evt, TEntity entity, IAggregatorStreamContext ctx);
}

/// <summary>
/// Event Aggregation Logic for a specific Event and a Specific Entity
/// </summary>
/// <typeparam name="TEntity">The Entity the Event shall be aggregated on</typeparam>
public interface IEventAggregator<TEntity>
    where TEntity : class
{
  Task<TEntity?> ApplyAsync(JObject evt, TEntity entity, IAggregatorStreamContext ctx);
}
