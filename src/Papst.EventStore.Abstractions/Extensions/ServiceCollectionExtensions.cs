using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Papst.EventStore.Abstractions.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add Event Stream Applier
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddEventStreamApplier(this IServiceCollection services, params Assembly[] assemblies)
        {
            if (assemblies.Length == 0)
            {
                throw new NotSupportedException("At least one Assembly to scan is necessary!");
            }

            Type interfaceType = typeof(IEventApplier<,>);

            // scan the assemblies for types that implement IEventApplier<>
            var types = assemblies
                .SelectMany(a => a.DefinedTypes)
                .Distinct()
                .Where(t =>
                    t.IsClass &&
                    t.ImplementedInterfaces.Any(itrf => itrf.IsGenericType && itrf.GetGenericTypeDefinition() == interfaceType)
                );

            foreach (TypeInfo type in types)
            {
                // find generic type argument to interfaces
                foreach (var itrf in type.ImplementedInterfaces.Where(itrf => itrf.IsGenericType && itrf.GetGenericTypeDefinition() == interfaceType))
                {
                    services.AddTransient(itrf, type.AsType());
                }
            }
            return services.AddTransient(typeof(IEventStreamApplier<>), typeof(DependencyInjectionEventApplier<>));
        }
    }
}
