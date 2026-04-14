using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Papst.EventStore.Aggregation;

/// <summary>
/// Event Aggregation Logic for a specific Event and a Specific Entity
/// </summary>
/// <typeparam name="TEntity">The Entity the Event shall be aggregated on</typeparam>
/// <typeparam name="TEvent">The Event that shall be aggregated</typeparam>
public interface IEventAggregator<TEntity, TEvent> : IEventAggregator<TEntity>
    where TEntity : class
{
  /// <summary>
  /// Applies the <see cref="TEvent"/> to the Entity <see cref="TEntity"/>
  /// </summary>
  /// <param name="evt"></param>
  /// <param name="entity"></param>
  /// <param name="ctx"></param>
  /// <returns></returns>
  ValueTask<TEntity?> ApplyAsync(TEvent evt, TEntity entity, IAggregatorStreamContext ctx);
}

/// <summary>
/// Event Aggregation Logic for a specific Event and a Specific Entity
/// </summary>
/// <typeparam name="TEntity">The Entity the Event shall be aggregated on</typeparam>
public interface IEventAggregator<TEntity>
    where TEntity : class
{
  /// <summary>
  /// Applies the Event as <see cref="JsonNode"/> to the Entity <see cref="TEntity"/>
  /// </summary>
  /// <param name="evt"></param>
  /// <param name="entity"></param>
  /// <param name="ctx"></param>
  /// <returns></returns>
  [RequiresUnreferencedCode("JSON deserialization of event data may require types that are not statically referenced.")]
  [RequiresDynamicCode("JSON deserialization of event data may require runtime code generation.")]
  ValueTask<TEntity?> ApplyAsync(JsonNode evt, TEntity entity, IAggregatorStreamContext ctx);
}
