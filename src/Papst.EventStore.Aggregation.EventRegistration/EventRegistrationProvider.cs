using Microsoft.Extensions.DependencyInjection;

namespace Papst.EventStore.Aggregation.EventRegistration;

public static class EventRegistrationProvider
{
  /// <summary>
  /// Add a <see cref="IEventStreamAggregator{TTargetType}"/> Event Stream Aggregator
  /// based on code generated event registration
  /// </summary>
  /// <param name="services"></param>
  /// <returns></returns>
  public static IServiceCollection AddRegisteredEventAggregation(this IServiceCollection services)
    => services.AddTransient(typeof(IEventStreamAggregator<>), typeof(EventRegistrationEventAggregator<>));
}
