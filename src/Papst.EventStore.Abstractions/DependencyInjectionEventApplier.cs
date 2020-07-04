using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Papst.EventStore.Abstractions
{
    internal class DependencyInjectionEventApplier<TEntity> : IEventStreamAggregator<TEntity>
        where TEntity : class, IEntity, new()
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<DependencyInjectionEventApplier<TEntity>> _logger;
        private readonly IOptions<EventStoreOptions> _options;

        public DependencyInjectionEventApplier(IServiceProvider services, ILogger<DependencyInjectionEventApplier<TEntity>> logger, IOptions<EventStoreOptions> options)
        {
            _services = services;
            _logger = logger;
            _options = options;
        }

        public async Task<TEntity> ApplyAsync(IEventStream stream)
        {
            _logger.LogDebug("Creating new Entity");
            // create the Entity using the StartVersion - 1 because the Aggregator will increment it after applying the first Event
            return await ApplyAsync(stream, new TEntity() { Version = _options.Value.StartVersion == 0 ? 0 : _options.Value.StartVersion - 1 }).ConfigureAwait(false);
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
            Type eventApplierType = typeof(IEventAggregator<,>);

            // the entity type as first type argument to the IEventApplier<,>
            Type entityType = target.GetType();

            bool isFirstEvent = true;

            foreach (var evt in stream.Stream)
            {
                try
                {
                    IEventAggregator<TEntity> applier = _services.GetRequiredService(eventApplierType.MakeGenericType(entityType, evt.DataType)) as IEventAggregator<TEntity>;
                    _logger.LogDebug("Applying {Event} to {Entity}", evt.Id, target);
                    ulong previousVersion = target.Version;
                    target = await applier.ApplyAsync(evt.Data, target).ConfigureAwait(false);

                    if (target == null)
                    {
                        _logger.LogInformation("Entity has been deleted at {Version}", previousVersion);
                        break;
                    }
                    else if (target.Version == previousVersion && !isFirstEvent)
                    {
                        // --> Version is incremented when not done by custom logic and its not the first event
                        target.Version++;
                    }

                    isFirstEvent = false;
                }
                catch (InvalidOperationException exc)
                {
                    _logger.LogError(exc, "Unable to retrieve IEventAggregator for {EntityType} and {EventType}", entityType, evt.DataType);
                }
            }

            return target;
        }
    }
}
