using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Papst.EventStore.Abstractions.EventRegistration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Papst.EventStore.Abstractions.EventAggregation.EventRegistration;

internal class EventRegistrationEventAggregator<TEntity> : IEventStreamAggregator<TEntity>
  where TEntity : class, IEntity, new()
{
  private readonly ILogger<EventRegistrationEventAggregator<TEntity>> _logger;
  private readonly IOptions<EventStoreOptions> _options;
  private readonly IServiceProvider _serviceProvider;
  private readonly IEventTypeProvider _eventTypeProvider;

  public EventRegistrationEventAggregator(
    ILogger<EventRegistrationEventAggregator<TEntity>> logger,
    IOptions<EventStoreOptions> options,
    IServiceProvider serviceProvider,
    IEventTypeProvider eventTypeProvider)
  {
    _logger = logger;
    _options = options;
    _serviceProvider = serviceProvider;
    _eventTypeProvider = eventTypeProvider;
  }

  /// <inheritdoc/>
  public async Task<TEntity> AggregateAsync(IEventStream stream)
    => await AggregateAsync(stream, stream.Stream[stream.Stream.Count - 1].Version).ConfigureAwait(false);

  /// <inheritdoc/>
  public async Task<TEntity> AggregateAsync(IEventStream stream, ulong targetVersion)
  {
    Logger.CreatingNewEntity(_logger, typeof(TEntity).Name, stream.StreamId);
    // create the Entity using the StartVersion - 1 because the Aggregator will increment it after applying the first Event
    return await AggregateAsync(
      stream,
      new TEntity() { Version = _options.Value.StartVersion == 0 ? 0 : _options.Value.StartVersion - 1 }
    ).ConfigureAwait(false);
  }

  /// <inheritdoc/>
  public async Task<TEntity> AggregateAsync(IEventStream stream, TEntity target)
    => await AggregateAsync(stream, target, stream.Stream[stream.Stream.Count - 1].Version).ConfigureAwait(false);

  /// <inheritdoc/>
  public async Task<TEntity> AggregateAsync(IEventStream stream, TEntity originalTarget, ulong targetVersion)
  {
    TEntity? target = originalTarget;

    Type entityType = typeof(TEntity);
    Type aggregatorType = typeof(IEventAggregator<,>);

    bool isFirstEvent = true;
    bool hasBeenDeleted = false;
    EventRegistrationEventAggregatorStreamContext context = new()
    {
      StreamId = stream.StreamId,
      TargetVersion = targetVersion,
      CurrentVersion = stream.Stream[0].Version,
      StreamCreated = stream.Stream[0].Time
    };

    foreach (var evt in stream.Stream.Where(doc => doc.Version <= targetVersion))
    {
      try
      {
        Type eventType = _eventTypeProvider.GetEventType(evt.DataType);
        Logger.ApplyingEvent(_logger, evt.DataType, entityType.Name, stream.StreamId);
        IEventAggregator<TEntity> aggregator = (_serviceProvider.GetRequiredService(aggregatorType.MakeGenericType(entityType, eventType)) as IEventAggregator<TEntity>)!;

        ulong previousVersion = (hasBeenDeleted || target == null ? evt.Version : target.Version);

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
          Logger.EntityDeleted(_logger, stream.StreamId, evt.DataType, evt.Version);
          hasBeenDeleted = true;
        }
        else if (target.Version == previousVersion && (previousVersion > _options.Value.StartVersion || !isFirstEvent))
        {
          target.Version++;
        }
        isFirstEvent = false;
      }
      catch (InvalidOperationException exc)
      {
        Logger.EventAggregatorNotRegistered(_logger, exc, entityType.Name, evt.DataType);
        throw;
      }
    }
    return target;
  }
}

public partial class Logger
{
  private const string EventName = "Papst.EventStore";

  [LoggerMessage(EventId = 100_000, EventName = EventName, Level = LogLevel.Debug, Message = "Creating new Entity for Entity {Type} and Stream {StreamId}")]
  public static partial void CreatingNewEntity(ILogger logger, string type, Guid streamId);

  [LoggerMessage(EventId = 100_001, EventName = EventName, Level = LogLevel.Debug, Message = "Applying {EventName} to {Entity} with Id {StreamId}")]
  public static partial void ApplyingEvent(ILogger logger, string eventName, string entity, Guid streamId);

  [LoggerMessage(EventId = 100_002, EventName = EventName, Level = LogLevel.Information, Message = "Entity in Stream {StreamId} has been deleted by Event {EventName} at Version {Version}")]
  public static partial void EntityDeleted(ILogger logger, Guid streamId, string eventName, ulong version);

  [LoggerMessage(EventId = 100_003, EventName = EventName, Level = LogLevel.Error, Message = "Event Aggregator not registered in Dependency Injection for Entity {EntityType} and Event {EventName}")]
  public static partial void EventAggregatorNotRegistered(ILogger logger, Exception exc, string entityType, string eventName);
}
