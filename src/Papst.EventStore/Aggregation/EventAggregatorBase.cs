using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Papst.EventStore.Aggregation;

public abstract class EventAggregatorBase<TEntity, TEvent> : IEventAggregator<TEntity, TEvent>
  where TEntity : class
{
  /// <inheritdoc cref="IEventAggregator{TEntity,TEvent}"/>
  [RequiresUnreferencedCode("JSON deserialization of TEvent may require unreferenced code. Use a JsonSerializerContext for AOT compatibility.")]
  [RequiresDynamicCode("JSON deserialization of TEvent may require dynamic code generation. Use a JsonSerializerContext for AOT compatibility.")]
  public async ValueTask<TEntity?> ApplyAsync(JsonNode evt, TEntity entity, IAggregatorStreamContext ctx) => await ApplyAsync(
    evt.Deserialize<TEvent>(ctx.JsonSerializerOptions) ?? throw new NotSupportedException($"Could not parse Event {evt}"),
    entity,
    ctx);

  /// <inheritdoc cref="IEventAggregator{TEntity,TEvent}"/>
  public abstract ValueTask<TEntity?> ApplyAsync(TEvent evt, TEntity entity, IAggregatorStreamContext ctx);
  
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
  /// Executes the <paramref name="setter"/> action when <paramref name="value"/>.HasValue is true
  /// </summary>
  /// <typeparam name="TProperty"></typeparam>
  /// <param name="value"></param>
  /// <param name="setter"></param>
  protected void Update<TProperty>(TProperty? value, Action<TProperty> setter) where TProperty : struct
  {
    if (value.HasValue)
    {
      setter.Invoke(value.Value);
    }
  }
  
  /// <summary>
  /// Executes the <paramref name="setter"/> action when <paramref name="value"/>.HasValue is true
  /// </summary>
  /// <typeparam name="TProperty"></typeparam>
  /// <param name="value"></param>
  /// <param name="setter"></param>
  protected void Update<TProperty>(TProperty? value, Action<TProperty?> setter) where TProperty : struct
  {
    if (value.HasValue)
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
  /// This overload is for nullable value types.
  /// Executes the <paramref name="setter"/> action when <paramref name="value"/>.HasValue is true.
  /// </summary>
  /// <param name="setter"></param>
  /// <param name="value"></param>
  /// <typeparam name="TProperty"></typeparam>
  protected void SetIfNotNull<TProperty>(Action<TProperty> setter, TProperty? value) where TProperty : struct
  {
    if (value.HasValue)
    {
      setter(value.Value);
    }
  }
  
  /// <summary>
  /// This overload is for nullable value types.
  /// Executes the <paramref name="setter"/> action when <paramref name="value"/>.HasValue is true.
  /// </summary>
  /// <param name="setter"></param>
  /// <param name="value"></param>
  /// <typeparam name="TProperty"></typeparam>
  protected void SetIfNotNull<TProperty>(Action<TProperty?> setter, TProperty? value) where TProperty : struct
  {
    if (value.HasValue)
    {
      setter(value);
    }
  }

  /// <summary>
  /// Returns the given Entity wrapped in a Task
  /// </summary>
  /// <param name="entity"></param>
  /// <returns></returns>
  protected ValueTask<TEntity?> AsTask(TEntity? entity) => ValueTask.FromResult(entity);
}
