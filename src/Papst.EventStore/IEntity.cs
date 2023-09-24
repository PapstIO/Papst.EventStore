using Papst.EventStore.Aggregation;

namespace Papst.EventStore;

/// <summary>
/// Code Contract for the Entity
/// </summary>
public interface IEntity
{
  /// <summary>
  /// Current Version of the Entity
  /// The property is updated by the <see cref="IEventAggregator{TEntity,TEvent}"/> 
  /// </summary>
  ulong Version { get; set; }
}
