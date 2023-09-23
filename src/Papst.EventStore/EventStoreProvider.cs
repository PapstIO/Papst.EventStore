using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.EventRegistration;

namespace Papst.EventStore;
public static class EventStoreProvider
{
  public static IServiceCollection AddEventRegistrationTypeProvider(this IServiceCollection services)
    => services.AddSingleton<IEventTypeProvider, EventRegistrationTypeProvider>();
}
