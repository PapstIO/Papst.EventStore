using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Papst.EventStore.Abstractions
{
    internal class DependencyInjectionEventApplier<TEntity> : IEventStreamApplier<TEntity>
        where TEntity : class, new()
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<DependencyInjectionEventApplier<TEntity>> _logger;

        public DependencyInjectionEventApplier(IServiceProvider services, ILogger<DependencyInjectionEventApplier<TEntity>> logger)
        {
            _services = services;
            _logger = logger;
        }

        public TEntity Apply(IEventStream stream)
        {
            _logger.LogDebug("Creating new Entity");
            return Apply(stream, new TEntity());
        }

        /// <summary>
        /// Apply the Stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public TEntity Apply(IEventStream stream, TEntity target)
        {
            // the interface type to retrieve from DI
            Type eventApplierType = typeof(IEventApplier<,>);

            // the entity type as first type argument to the IEventApplier<,>
            Type entityType = target.GetType();

            foreach (var evt in stream.Stream)
            {
                try
                {
                    IEventApplier<TEntity> applier = _services.GetRequiredService(eventApplierType.MakeGenericType(entityType, evt.DataType)) as IEventApplier<TEntity>;
                    _logger.LogDebug("Applying {Event} to {Entity}", evt.Id, target);
                    applier.ApplyAsync(evt.Data, target);
                }
                catch (InvalidOperationException exc)
                {
                    _logger.LogError(exc, "Unable to retrieve IEventApplier for {EntityType} and {EventType}", entityType, evt.DataType);
                }
            }

            return target;
        }
    }
}
