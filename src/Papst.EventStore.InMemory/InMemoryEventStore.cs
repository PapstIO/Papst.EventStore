using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Papst.EventStore.Exceptions;

namespace Papst.EventStore.InMemory;

public class InMemoryEventStore : IEventStore
{
  private readonly Dictionary<Guid, InMemoryEventStream> _streams = new();
  private readonly TimeProvider _timeProvider;
  private readonly IEventTypeProvider _eventTypeProvider;

  public InMemoryEventStore(TimeProvider timeProvider, IEventTypeProvider eventTypeProvider)
  {
    _timeProvider = timeProvider;
    _eventTypeProvider = eventTypeProvider;
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
    if (_streams.ContainsKey(streamId))
    {
      throw new EventStreamAlreadyExistsException(streamId, "Stream already exists");
    }

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
    _streams.Add(streamId, stream);
    return Task.FromResult<IEventStream>(stream);
  }
}
