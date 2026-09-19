using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.EntityFrameworkCore.Database;
using Papst.EventStore.Pipeline;

namespace Papst.EventStore.EntityFrameworkCore;

public static class EntityFrameworkCoreEventStoreProvider
{
  public static IServiceCollection AddEntityFrameworkCoreEventStore(
    this IServiceCollection services,
    Action<DbContextOptionsBuilder> configure
  )
  {
    services.AddTransient<IEventStore, EntityFrameworkEventStore>();
    services.AddDbContext<EventStoreDbContext>(configure);
    services.AddEventStorePipeline();
    return services;
  }
}
