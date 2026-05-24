using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.EntityFrameworkCore.Database;

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
    return services;
  }
}
