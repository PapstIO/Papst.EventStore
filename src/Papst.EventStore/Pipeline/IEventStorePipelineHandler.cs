namespace Papst.EventStore.Pipeline;

/// <summary>
/// A pipeline handler ("middleware" / "behaviour") that participates in an Event
/// Store pipeline for the given context type. Handlers are executed in
/// registration order, each wrapping the next.
/// </summary>
/// <typeparam name="TContext">The pipeline context type.</typeparam>
public interface IEventStorePipelineHandler<in TContext>
  where TContext : EventStorePipelineContext
{
  /// <summary>
  /// Handles the pipeline operation. Implementations MUST call <c>await next()</c>
  /// to continue the pipeline, or throw to short-circuit it.
  /// </summary>
  /// <param name="context">The pipeline context.</param>
  /// <param name="next">The continuation of the pipeline.</param>
  /// <param name="cancellationToken"></param>
  Task HandleAsync(TContext context, EventStorePipelineDelegate next, CancellationToken cancellationToken);
}
