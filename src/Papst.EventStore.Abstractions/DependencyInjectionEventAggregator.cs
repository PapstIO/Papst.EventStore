using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Papst.EventStore.Abstractions
{
    internal class DependencyInjectionEventAggregator<TEntity> : IEventStreamAggregator<TEntity>
        where TEntity : class, IEntity, new()
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<DependencyInjectionEventAggregator<TEntity>> _logger;
        private readonly IOptions<EventStoreOptions> _options;

        public DependencyInjectionEventAggregator(IServiceProvider services, ILogger<DependencyInjectionEventAggregator<TEntity>> logger, IOptions<EventStoreOptions> options)
        {
            _services = services;
            _logger = logger;
            _options = options;
        }

        public async Task<TEntity> AggregateAsync(IEventStream stream)
        {
            _logger.LogDebug("Creating new Entity");
            // create the Entity using the StartVersion - 1 because the Aggregator will increment it after applying the first Event
            return await AggregateAsync(stream, new TEntity() { Version = _options.Value.StartVersion == 0 ? 0 : _options.Value.StartVersion - 1 }).ConfigureAwait(false);
        }

        /// <summary>
        /// Apply the Stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public async Task<TEntity> AggregateAsync(IEventStream stream, TEntity originalTarget)
        {
            TEntity? target = originalTarget;
            // the interface type to retrieve from DI
            Type eventApplierType = typeof(IEventAggregator<,>);

            // the entity type as first type argument to the IEventApplier<,>
            Type entityType = target.GetType();

            bool isFirstEvent = true;
            DependencyInjectionEventAggregatorStreamContext context = new DependencyInjectionEventAggregatorStreamContext
            {
                StreamId = stream.StreamId,
                TargetVersion = stream.Stream[stream.Stream.Count - 1].Version,
                CurrentVersion = stream.Stream[0].Version,
                StreamCreated = stream.Stream[0].Time
            };

            bool hasBeenDeleted = false;

            foreach (var evt in stream.Stream)
            {
                try
                {
                    // retrieve the Aggregator
                    IEventAggregator<TEntity> applier = (_services.GetRequiredService(eventApplierType.MakeGenericType(entityType, evt.DataType)) as IEventAggregator<TEntity>)!;
                    _logger.LogDebug("Applying {Event} to {Entity}", evt.Id, target);

                    if (target == null)
                    {
                        target = new TEntity();
                    }

                    ulong previousVersion = (hasBeenDeleted ? evt.Version : target.Version);

                    // create a context for the current event which is aggregated
                    context = context with
                    {
                        CurrentVersion = evt.Version,
                        EventTime = evt.Time
                    };

                    // reset hasBeenDeleted flag
                    if (hasBeenDeleted)
                    {
                        hasBeenDeleted = false;
                    }

                    target = await applier.ApplyAsync(evt.Data, target, context).ConfigureAwait(false);

                    if (target == null)
                    {
                        _logger.LogInformation("Entity has been deleted at {Version}", previousVersion);
                        hasBeenDeleted = true;
                    }
                    else if (target.Version == previousVersion && (previousVersion > _options.Value.StartVersion || !isFirstEvent))
                    {
                        // --> Version is incremented when not done by custom logic and its not the first event
                        // --> When the Version is already greater StartVersion, it is rebuild from a SnapShot -> increment the version then
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
