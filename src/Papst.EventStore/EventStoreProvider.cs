using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.EventRegistration;

namespace Papst.EventStore;
public static class EventStoreProvider
{
  /// <summary>
  /// Add The <see cref="IEventTypeProvider"/> based on EventRegistration to the DI Container
  /// </summary>
  /// <param name="services"></param>
  /// <returns></returns>
  public static IServiceCollection AddEventRegistrationTypeProvider(this IServiceCollection services)
    => services.AddSingleton<IEventTypeProvider, EventRegistrationTypeProvider>();
}
