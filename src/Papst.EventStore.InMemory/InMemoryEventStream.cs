using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Papst.EventStore.Documents;

namespace Papst.EventStore.InMemory;

internal class InMemoryEventStream : IEventStream
{
  private readonly List<EventStreamDocument> _events = new();
  private readonly TimeProvider _tp;
  private readonly string _targetType;
  private readonly IEventTypeProvider _typeProvider;

  public InMemoryEventStream(
    Guid streamId,
    ulong version,
    DateTimeOffset created,
    EventStreamMetaData metaData,
    TimeProvider tp,
    string targetType,
    IEventTypeProvider typeProvider)
  {
    StreamId = streamId;
    Version = version;
    Created = created;
    MetaData = metaData;
    _tp = tp;
    _targetType = targetType;
    _typeProvider = typeProvider;
  }

  public Guid StreamId { get; }
  public ulong Version { get; }
  public DateTimeOffset Created { get; }
  public ulong? LatestSnapshotVersion => _events
    .Where(e => e.DocumentType == EventStreamDocumentType.Snapshot)
    .MaxBy(e => e.Version)?
    .Version;
  public EventStreamMetaData MetaData { get; }

  public Task<EventStreamDocument?> GetLatestSnapshot(CancellationToken cancellationToken = default) 
    => Task.FromResult(_events.LastOrDefault(e => e.DocumentType == EventStreamDocumentType.Snapshot));

  public Task AppendAsync<TEvent>(Guid id, TEvent evt, EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default) where TEvent : notnull
  {
    string name = _typeProvider.ResolveType(typeof(TEvent));
    _events.Add(new EventStreamDocument()
    {
      StreamId = StreamId,
      Version = Version + 1,
      Time = _tp.GetLocalNow(),
      DataType = name,
      Data = JObject.FromObject(evt),
      DocumentType = EventStreamDocumentType.Event,
      MetaData = metaData ?? new EventStreamMetaData(),
      TargetType = _targetType,
      Id = id,
      Name = name
    });

    return Task.CompletedTask;
  }

  public Task AppendSnapshotAsync<TEntity>(Guid id, TEntity entity, EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default) where TEntity : notnull
  {
    _events.Add(new EventStreamDocument()
    {
      StreamId = StreamId,
      Version = Version + 1,
      Time = _tp.GetLocalNow(),
      DataType = _targetType,
      Data = JObject.FromObject(entity),
      DocumentType = EventStreamDocumentType.Snapshot,
      MetaData = metaData ?? new EventStreamMetaData(),
      TargetType = _targetType,
      Id = id,
      Name = _targetType
    });

    return Task.CompletedTask;
  }

  public Task<IEventStoreTransactionAppender> CreateTransactionalBatchAsync()
  {
    return Task.FromResult<IEventStoreTransactionAppender>(
      new InMemoryTransactionalBatch(_events, _typeProvider, _tp, StreamId, _targetType));
  }

  public IAsyncEnumerable<EventStreamDocument> ListAsync(ulong startVersion = 0,
    CancellationToken cancellationToken = default)
  {
    return _events.Where(evt => evt.Version >= startVersion).ToAsyncEnumerable();
  }

  public IAsyncEnumerable<EventStreamDocument> ListAsync(ulong startVersion, ulong endVersion,
    CancellationToken cancellationToken = default)
  {
    return _events
      .Where(evt => evt.Version >= startVersion && evt.Version <= endVersion)
      .ToAsyncEnumerable();
  }

  public IAsyncEnumerable<EventStreamDocument> ListDescendingAsync(ulong endVersion, ulong startVersion,
    CancellationToken cancellationToken = default)
  {
    return _events.Where(evt => evt.Version >= startVersion && evt.Version <= endVersion)
      .OrderByDescending(evt => evt.Version)
      .ToAsyncEnumerable();
  }

  public IAsyncEnumerable<EventStreamDocument> ListDescendingAsync(ulong endVersion,
    CancellationToken cancellationToken = default)
  {
    return _events.Where(evt => evt.Version <= endVersion)
      .OrderByDescending(evt => evt.Version)
      .ToAsyncEnumerable();
  }
  
  
  
}

internal class InMemoryTransactionalBatch(
  List<EventStreamDocument> events,
  IEventTypeProvider typeProvider,
  TimeProvider tp,
  Guid streamId,
  string targetType) : IEventStoreTransactionAppender
{
  private readonly List<Action> _actions = [];
    
  public IEventStoreTransactionAppender Add<TEvent>(Guid id, TEvent evt, EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default) where TEvent : notnull
  {
    string name = typeProvider.ResolveType(typeof(TEvent));
    _actions.Add(() => events.Add(new EventStreamDocument()
    {
      StreamId = streamId,
      Version = events.Max(e => e.Version) + 1,
      Time = tp.GetLocalNow(),
      DataType = name,
      Data = JObject.FromObject(evt),
      DocumentType = EventStreamDocumentType.Event,
      MetaData = metaData ?? new EventStreamMetaData(),
      TargetType = targetType,
      Id = id,
      Name = name
    }));

    return this;
  }

  public Task CommitAsync(CancellationToken cancellationToken = default)
  {
    foreach (var action in _actions)
    {
      action();
    }
    return Task.CompletedTask;
  }
}
