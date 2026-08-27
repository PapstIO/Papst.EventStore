using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Papst.EventStore.Pipeline;

/// <summary>
/// Dependency Injection extensions for the Event Store pipeline.
/// </summary>
public static class EventStorePipelineProvider
{
  /// <summary>
  /// Registers the default open-generic <see cref="IEventStorePipeline{TContext}"/>.
  /// Called by every store's registration; safe to call multiple times.
  /// </summary>
  public static IServiceCollection AddEventStorePipeline(this IServiceCollection services)
  {
    services.TryAddSingleton(typeof(IEventStorePipeline<>), typeof(EventStorePipeline<>));
    return services;
  }

  /// <summary>
  /// Registers a handler for the <see cref="EventAppendContext"/> append pipeline.
  /// The order of registration is the order of execution (first registered runs
  /// outermost), matching the ASP.NET Core middleware pipeline.
  /// </summary>
  /// <typeparam name="THandler">The handler type.</typeparam>
  public static IServiceCollection AddAppendPipelineHandler<THandler>(this IServiceCollection services)
    where THandler : class, IEventStorePipelineHandler<EventAppendContext>
  {
    services.AddEventStorePipeline();
    services.AddSingleton<IEventStorePipelineHandler<EventAppendContext>, THandler>();
    return services;
  }
}
