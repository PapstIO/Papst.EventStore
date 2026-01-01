using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Papst.EventStore.Documents;
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
  public async Task<TEntity?> AggregateAsync(IEventStream stream, CancellationToken cancellationToken)
    => await AggregateAsync(stream, stream.Version, cancellationToken).ConfigureAwait(false);

  /// <inheritdoc/>
  public async Task<TEntity?> AggregateAsync(IEventStream stream, ulong targetVersion, CancellationToken cancellationToken)
  {
    _logger.CreatingNewEntity(typeof(TEntity).Name, stream.StreamId);
    // create the Entity using the StartVersion - 1 because the Aggregator will increment it after applying the first Event
    return await AggregateAsync(
      stream,
      new TEntity() { Version = 0 },
      targetVersion,
      cancellationToken
    ).ConfigureAwait(false);
  }

  /// <inheritdoc/>
  public async Task<TEntity?> AggregateAsync(IEventStream stream, TEntity target, CancellationToken cancellationToken)
    => await AggregateAsync(stream, target, stream.Version, cancellationToken).ConfigureAwait(false);

  /// <inheritdoc/>
  public async Task<TEntity?> AggregateAsync(IEventStream stream, TEntity originalTarget, ulong targetVersion, CancellationToken cancellationToken)
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

        if (targetVersion > 0 && target.Version == targetVersion)
        {
          // if the current version is already the target version, stop the aggregation,
          // but only if the target version is greater than 0 it should apply the creation event
          break;
        }

        _logger.ApplyingEvent(evt.DataType, entityType.Name, stream.StreamId);


        if (evt.DocumentType == EventStreamDocumentType.Snapshot)
        {
          // when the event is a snapshot, just use it as target
          target = evt.Data.ToObject<TEntity>() ?? target ?? new();
          // update the targets Version to match the current Snapshot
          target.Version = context.CurrentVersion;

          continue;
        }
        Type eventType = _eventTypeProvider.ResolveIdentifier(evt.DataType);
        IEventAggregator<TEntity> aggregator = (_serviceProvider.GetRequiredService(aggregatorType.MakeGenericType(entityType, eventType)) as IEventAggregator<TEntity>)!;


        if (hasBeenDeleted)
        {
          hasBeenDeleted = false;
        }

        target = await aggregator.ApplyAsync(evt.Data, target, context).ConfigureAwait(false);

        if (target == null)
        {
          _logger.EntityDeleted(stream.StreamId, evt.DataType, evt.Version);
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
        _logger.EventAggregatorNotRegistered(exc, entityType.Name, evt.DataType);
        throw new EventStreamAggregatorNotRegisteredException(exc, stream.StreamId, entityType.Name, evt.DataType);
      }
    }
    return target;
  }
}
