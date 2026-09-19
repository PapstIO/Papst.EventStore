using System.Linq;

namespace Papst.EventStore.Pipeline;

/// <summary>
/// Default <see cref="IEventStorePipeline{TContext}"/> that folds the registered
/// handlers around the terminal. Handlers run in registration order, the first
/// registered being the outermost.
/// </summary>
/// <typeparam name="TContext">The pipeline context type.</typeparam>
internal sealed class EventStorePipeline<TContext> : IEventStorePipeline<TContext>
  where TContext : EventStorePipelineContext
{
  private readonly IReadOnlyList<IEventStorePipelineHandler<TContext>> _handlers;

  public EventStorePipeline(IEnumerable<IEventStorePipelineHandler<TContext>> handlers)
    => _handlers = handlers.ToList();

  public Task ExecuteAsync(TContext context, EventStorePipelineDelegate terminal, CancellationToken cancellationToken)
  {
    EventStorePipelineDelegate next = terminal;
    for (int i = _handlers.Count - 1; i >= 0; i--)
    {
      IEventStorePipelineHandler<TContext> handler = _handlers[i];
      EventStorePipelineDelegate localNext = next;
      next = () => handler.HandleAsync(context, localNext, cancellationToken);
    }

    return next();
  }
}
