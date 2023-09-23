using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Papst.EventStore.Abstractions;
using Papst.EventStore.Abstractions.EventAggregation.EventRegistration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Papst.EventStore.Aggregation.EventRegistration;

internal class EventRegistrationEventAggregator<TEntity> : IEventStreamAggregator<TEntity>
  where TEntity : class, IEntity, new()
{
  private readonly ILogger<EventRegistrationEventAggregator<TEntity>> _logger;
  private readonly IServiceProvider _serviceProvider;
  private readonly IEventTypeProvider _eventTypeProvider;

  public EventRegistrationEventAggregator(
    ILogger<EventRegistrationEventAggregator<TEntity>> logger,
    IServiceProvider serviceProvider,
    IEventTypeProvider eventTypeProvider)
  {
    _logger = logger;
    _serviceProvider = serviceProvider;
    _eventTypeProvider = eventTypeProvider;
  }

  /// <inheritdoc/>
  public async Task<TEntity> AggregateAsync(IEventStream stream, CancellationToken cancellationToken)
    => await AggregateAsync(stream, stream.Version, cancellationToken).ConfigureAwait(false);

  /// <inheritdoc/>
  public async Task<TEntity> AggregateAsync(IEventStream stream, ulong targetVersion, CancellationToken cancellationToken)
  {
    Logging.CreatingNewEntity(_logger, typeof(TEntity).Name, stream.StreamId);
    // create the Entity using the StartVersion - 1 because the Aggregator will increment it after applying the first Event
    return await AggregateAsync(
      stream,
      new TEntity() { Version = 0 },
      cancellationToken
    ).ConfigureAwait(false);
  }

  /// <inheritdoc/>
  public async Task<TEntity> AggregateAsync(IEventStream stream, TEntity target, CancellationToken cancellationToken)
    => await AggregateAsync(stream, target, stream.Version, cancellationToken).ConfigureAwait(false);

  /// <inheritdoc/>
  public async Task<TEntity> AggregateAsync(IEventStream stream, TEntity originalTarget, ulong targetVersion, CancellationToken cancellationToken)
  {
    TEntity? target = originalTarget;

    Type entityType = typeof(TEntity);
    Type aggregatorType = typeof(IEventAggregator<,>);

    bool hasBeenDeleted = false;
    EventRegistrationEventAggregatorStreamContext context = new(
      stream.StreamId,
      originalTarget.Version,
      targetVersion,
      stream.Created,
      stream.Created);

    await foreach (var evt in stream.ListAsync(originalTarget.Version, cancellationToken))
    {
      try
      {
        Type eventType = _eventTypeProvider.ResolveIdentifier(evt.DataType);
        Logging.ApplyingEvent(_logger, evt.DataType, entityType.Name, stream.StreamId);
        IEventAggregator<TEntity> aggregator = (_serviceProvider.GetRequiredService(aggregatorType.MakeGenericType(entityType, eventType)) as IEventAggregator<TEntity>)!;

        ulong previousVersion = hasBeenDeleted || target == null ? evt.Version : target.Version;

        if (target == null)
        {
          target = new TEntity()
          {
            Version = previousVersion
          };
        }

        context = context with
        {
          CurrentVersion = evt.Version,
          EventTime = evt.Time
        };

        if (hasBeenDeleted)
        {
          hasBeenDeleted = false;
        }

        target = await aggregator.ApplyAsync(evt.Data, target, context).ConfigureAwait(false);

        if (target == null)
        {
          Logging.EntityDeleted(_logger, stream.StreamId, evt.DataType, evt.Version);
          hasBeenDeleted = true;
        }
        else
        {
          // set the entity Version to the current event version
          target.Version = context.CurrentVersion;
        }
      }
      catch (InvalidOperationException exc)
      {
        Logging.EventAggregatorNotRegistered(_logger, exc, entityType.Name, evt.DataType);
        throw;
      }
    }
    return target;
  }
}
