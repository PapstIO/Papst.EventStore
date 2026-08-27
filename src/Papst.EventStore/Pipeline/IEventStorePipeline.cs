namespace Papst.EventStore.Pipeline;

/// <summary>
/// Executes the registered <see cref="IEventStorePipelineHandler{TContext}"/>
/// handlers around a terminal operation. With no handlers registered the terminal
/// is invoked directly, so the pipeline adds no overhead and preserves behaviour.
/// </summary>
/// <typeparam name="TContext">The pipeline context type.</typeparam>
public interface IEventStorePipeline<in TContext>
  where TContext : EventStorePipelineContext
{
  /// <summary>
  /// Runs the handler chain for <paramref name="context"/>, ending in
  /// <paramref name="terminal"/>.
  /// </summary>
  /// <param name="context">The pipeline context.</param>
  /// <param name="terminal">The terminal operation, e.g. the store persistence.</param>
  /// <param name="cancellationToken"></param>
  Task ExecuteAsync(TContext context, EventStorePipelineDelegate terminal, CancellationToken cancellationToken);
}
