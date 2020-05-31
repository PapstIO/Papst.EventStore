﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.Abstractions;
using Papst.EventStore.Abstractions.Extensions;
using System;

namespace Papst.EventStore.CosmosDb.Extensions
{
    /// <summary>
    /// Extensions for the <see cref="IServiceCollection"/> to add necessary services for the <see cref="CosmosEventStore"/>
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the Cosmos Database SQL Api EventStore
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IServiceCollection AddCosmosEventStore(this IServiceCollection services, IConfigurationSection config) => services
            .Configure<CosmosEventStoreOptions>(c => config.Bind(c))
            .AddCosmosServices(config)
            ;

        /// <summary>
        /// Internal: adds necessary services
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        private static IServiceCollection AddCosmosServices(this IServiceCollection services, IConfigurationSection config) => services
            .AddSingleton<EventStoreCosmosClient>()     // add the Cosmos Database Client
            .AddScoped<IEventStore, CosmosEventStore>() // Add the Cosmos EventStore
            .AddEventStreamApplier()                     // Add the EventStreamApplier
            ;
    }
}
