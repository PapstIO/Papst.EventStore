using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Papst.EventStore.Exceptions;

namespace Papst.EventStore.InMemory;

public class InMemoryEventStore : IEventStore
{
  private readonly ConcurrentDictionary<Guid, InMemoryEventStream> _streams = new();
  private readonly TimeProvider _timeProvider;
  private readonly IEventTypeProvider _eventTypeProvider;
  private readonly ILogger<InMemoryEventStore> _logger;

  public InMemoryEventStore(TimeProvider timeProvider, IEventTypeProvider eventTypeProvider, ILogger<InMemoryEventStore> logger)
  {
    _timeProvider = timeProvider;
    _eventTypeProvider = eventTypeProvider;
    _logger = logger;
  }

  public Task<IEventStream> GetAsync(Guid streamId, CancellationToken cancellationToken = default)
  {
    if (!_streams.TryGetValue(streamId, out InMemoryEventStream? stream))
    {
      throw new EventStreamNotFoundException(streamId,
        "InMemory Event Streams are not persisted, if you expect this stream here, you should create it first.");
    }

    return Task.FromResult<IEventStream>(stream);
  }

  public Task<ILowLevelEventStream> GetLowLevelAsync(Guid streamId, CancellationToken cancellationToken = default)
  {
    if (!_streams.TryGetValue(streamId, out InMemoryEventStream? stream))
    {
      throw new EventStreamNotFoundException(streamId,
        "InMemory Event Streams are not persisted, if you expect this stream here, you should create it first.");
    }

    return Task.FromResult<ILowLevelEventStream>(stream);
  }

  public Task<IEventStream> CreateAsync(Guid streamId, string targetTypeName,
    CancellationToken cancellationToken = default) =>
    CreateAsync(
      streamId,
      targetTypeName,
      null,
      null,
      null,
      null,
      null,
      cancellationToken);

  public Task<IEventStream> CreateAsync(Guid streamId, string targetTypeName, string? tenantId, string? userId,
    string? username,
    string? comment, Dictionary<string, string>? additionalMetaData, CancellationToken cancellationToken = default)
  {
    var stream = new InMemoryEventStream(
      streamId,
      0,
      _timeProvider.GetLocalNow(),
      new()
      {
        Additional = additionalMetaData,
        Comment = comment,
        TenantId = tenantId,
        UserId = userId,
        UserName = username
      },
      _timeProvider,
      targetTypeName,
      _eventTypeProvider
    );
    if (!_streams.TryAdd(streamId, stream))
    {
      throw new EventStreamAlreadyExistsException(streamId, "Stream already exists");
    }

    return Task.FromResult<IEventStream>(stream);
  }

  public Task DeleteAsync(Guid streamId, CancellationToken cancellationToken = default)
  {
    Logging.DeletingEventStream(_logger, streamId);

    if (!_streams.TryRemove(streamId, out _))
    {
      throw new EventStreamNotFoundException(streamId,
        "InMemory Event Streams are not persisted, if you expect this stream here, you should create it first.");
    }

    Logging.DeletedEventStream(_logger, streamId);
    return Task.CompletedTask;
  }
}
