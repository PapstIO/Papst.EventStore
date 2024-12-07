using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Papst.EventStore.InMemory;

public static class InMemoryEventStoreProvider
{
  /// <summary>
  /// Add the InMemory EventStore to the DI Container
  ///
  /// The InMemory store is used as a singleton and will be shared across the application
  /// </summary>
  /// <param name="services"></param>
  /// <returns></returns>
  public static IServiceCollection AddInMemoryEventStore(this IServiceCollection services)
  {
    services
      .AddSingleton<IEventStore, InMemoryEventStore>()
      ;
    
    services.TryAddSingleton(TimeProvider.System);
    return services;
  }
}
