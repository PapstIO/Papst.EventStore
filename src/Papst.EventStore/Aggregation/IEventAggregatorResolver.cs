namespace Papst.EventStore.Aggregation;

/// <summary>
/// Resolves an <see cref="IEventAggregator{TEntity}"/> for a given event <see cref="Type"/>,
/// without requiring runtime generic type construction.
/// </summary>
/// <typeparam name="TEntity">The entity type to aggregate</typeparam>
public interface IEventAggregatorResolver<TEntity>
  where TEntity : class
{
  /// <summary>
  /// Resolves the aggregator for the given <paramref name="eventType"/>.
  /// </summary>
  /// <param name="eventType">The CLR type of the event</param>
  /// <returns>The matching <see cref="IEventAggregator{TEntity}"/></returns>
  /// <exception cref="InvalidOperationException">Thrown when no aggregator is registered for the given event type</exception>
  IEventAggregator<TEntity> Resolve(Type eventType);
}
