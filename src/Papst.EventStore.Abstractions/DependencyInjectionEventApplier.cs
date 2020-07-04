using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Papst.EventStore.Abstractions
{
    internal class DependencyInjectionEventApplier<TEntity> : IEventStreamApplier<TEntity>
        where TEntity : class, IEntity, new()
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<DependencyInjectionEventApplier<TEntity>> _logger;

        public DependencyInjectionEventApplier(IServiceProvider services, ILogger<DependencyInjectionEventApplier<TEntity>> logger)
        {
            _services = services;
            _logger = logger;
        }

        public async Task<TEntity> ApplyAsync(IEventStream stream)
        {
            _logger.LogDebug("Creating new Entity");
            return await ApplyAsync(stream, new TEntity()).ConfigureAwait(false);
        }

        /// <summary>
        /// Apply the Stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public async Task<TEntity> ApplyAsync(IEventStream stream, TEntity target)
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
                    ulong previousVersion = target.Version;
                    target = await applier.ApplyAsync(evt.Data, target).ConfigureAwait(false);
                    
                    if (target == null)
                    {
                        _logger.LogInformation("Entity has been deleted at {Version}", previousVersion);
                        break;
                    }
                    else if (target.Version == previousVersion)
                    {
                        // increment the version only if it is not done by the event Applier
                        // note this will affect the version on creation: First Version will be incremented
                        target.Version++;
                    }
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
