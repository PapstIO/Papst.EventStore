using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Papst.EventStore.CosmosDb.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCosmosEventStore(this IServiceCollection services, IConfigurationSection config) => services
            .Configure<CosmosEventStoreOptions>(c => config.Bind(c))
            .AddCosmosServices()
            ;

        public static IServiceCollection AddCosmosEventStore(this IServiceCollection services, Action<CosmosEventStoreOptions> configure) => services
            .Configure(configure)
            .AddCosmosServices()
            ;

        private static IServiceCollection AddCosmosServices(this IServiceCollection services)
        {
            services.AddSingleton<EventStoreCosmosClient>();

            return services;
        }
    }
}
