using Newtonsoft.Json.Linq;

namespace Papst.EventStore.Aggregation;

public abstract class EventAggregatorBase<TEvent, TEntity> : IEventAggregator<TEntity, TEvent>
  where TEntity : class
{
  /// <inheritdoc cref="IEventAggregator{TEntity,TEvent}"/>
  public async Task<TEntity?> ApplyAsync(JObject evt, TEntity entity, IAggregatorStreamContext ctx) => await ApplyAsync(
    evt.ToObject<TEvent>() ?? throw new NotSupportedException($"Could not parse Event {evt}"),
    entity,
    ctx);

  /// <inheritdoc cref="IEventAggregator{TEntity,TEvent}"/>
  public abstract Task<TEntity?> ApplyAsync(TEvent evt, TEntity entity, IAggregatorStreamContext ctx);
  
  /// <summary>
  /// Executes the <paramref name="setter"/> action when <paramref name="value"/> is not null
  /// </summary>
  /// <typeparam name="TProperty"></typeparam>
  /// <param name="value"></param>
  /// <param name="setter"></param>
  protected void Update<TProperty>(TProperty? value, Action<TProperty> setter)
  {
    if (value is not null)
    {
      setter.Invoke(value);
    }
  }
  
  /// <summary>
  /// Executes the <paramref name="setter"/> action when <paramref name="value"/> is not null
  /// </summary>
  /// <typeparam name="TProperty"></typeparam>
  /// <param name="setter"></param>
  /// <param name="value"></param>
  protected void SetIfNotNull<TProperty>(Action<TProperty> setter, TProperty? value)
  {
    if (value is not null)
    {
      setter.Invoke(value);
    }
  }
  
  /// <summary>
  /// Returns the given Entity wrapped in a Task
  /// </summary>
  /// <param name="entity"></param>
  /// <returns></returns>
  protected Task<TEntity?> AsTask(TEntity? entity) => Task.FromResult(entity);
}
