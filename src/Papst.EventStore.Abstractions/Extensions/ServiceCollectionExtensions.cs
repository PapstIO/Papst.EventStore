using Microsoft.Extensions.DependencyInjection;

namespace Papst.EventStore.Abstractions.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add Event Stream Applier
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddEvenStreamApplier(this IServiceCollection services) 
            => services.AddTransient(typeof(IEventStreamApplier<>), typeof(TypeBasedEventStreamApplier<>));
    }
}
