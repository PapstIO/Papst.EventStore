using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Papst.EventStore.Abstractions.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Event Stream Aggregator
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddEventStreamAggregator(this IServiceCollection services, params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            throw new NotSupportedException("At least one Assembly to scan is necessary!");
        }

        Type interfaceType = typeof(IEventAggregator<,>);

        // scan the assemblies for types that implement IEventApplier<>
        var allTypes = assemblies
            .SelectMany(a => a.DefinedTypes)
            .Distinct()
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract
            )
            .ToList();
        var types = allTypes
            .Where(t => t.ImplementedInterfaces.Any(itrf => itrf.IsGenericType && itrf.GetGenericTypeDefinition() == interfaceType));

        foreach (TypeInfo type in types)
        {
            // find generic type argument to interfaces
            foreach (var itrf in type.ImplementedInterfaces.Where(itrf => itrf.IsGenericType && itrf.GetGenericTypeDefinition() == interfaceType))
            {
                services.AddTransient(itrf, type.AsType());
            }
        }
        return services.AddTransient(typeof(IEventStreamAggregator<>), typeof(DependencyInjectionEventAggregator<>));
    }

    /// <summary>
    /// Add Event Stream Applies
    /// Obsolete, use <see cref="AddEventStreamAggregator(IServiceCollection, Assembly[])"/> instead
    /// </summary>
    /// <param name="services"></param>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    [Obsolete("Use AddEventStreamAggregator instead")]
    public static IServiceCollection AddEventStreamApplier(this IServiceCollection services, params Assembly[] assemblies)
        => services.AddEventStreamAggregator(assemblies);
}
