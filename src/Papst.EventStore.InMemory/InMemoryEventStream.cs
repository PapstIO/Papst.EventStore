using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Papst.EventStore.Documents;
using Papst.EventStore.Pipeline;

namespace Papst.EventStore.InMemory;

/// <summary>
/// A document queued for appending before its version and signature are assigned.
/// </summary>
internal readonly record struct InMemoryAppendProto(
  Guid Id,
  string Name,
  JObject Data,
  EventStreamDocumentType Type,
  EventStreamMetaData? MetaData);

internal class InMemoryEventStream : IEventStream, ILowLevelEventStream
{
  // Guards _events: appends can race stream reads when the store is shared
  // across threads (e.g. an application host processing events on background
  // threads while tests seed streams).
  private readonly Lock _lock = new();

  // Serializes the read-head -> sign -> append sequence so the signature chain
  // stays consistent when the same stream is appended to concurrently. The gate
  // is held across the (async) pipeline execution, which the sync _lock cannot be.
  private readonly SemaphoreSlim _appendGate = new(1, 1);
  private readonly List<EventStreamDocument> _events = new();
  private readonly ulong _initialVersion;
  private readonly TimeProvider _tp;
  private readonly string _targetType;
  private readonly IEventTypeProvider _typeProvider;
  private readonly IEventStorePipeline<EventAppendContext> _pipeline;

  // Chain head + signing certificate mirrored from the latest appended document.
  private string? _latestSignature;

  public InMemoryEventStream(
    Guid streamId,
    ulong version,
    DateTimeOffset created,
    EventStreamMetaData metaData,
    TimeProvider tp,
    string targetType,
    IEventTypeProvider typeProvider,
    IEventStorePipeline<EventAppendContext> pipeline)
  {
    StreamId = streamId;
    _initialVersion = version;
    Created = created;
    MetaData = metaData;
    _tp = tp;
    _targetType = targetType;
    _typeProvider = typeProvider;
    _pipeline = pipeline;
  }

  public Guid StreamId { get; }

  public ulong Version
  {
    get
    {
      lock (_lock)
      {
        return VersionUnlocked;
      }
    }
  }

  private ulong VersionUnlocked => _events.Count == 0 ? _initialVersion : _events.Max(e => e.Version);

  // Version to assign to the next appended event. An empty stream starts at 0 so the
  // first event is version 0, matching the persisted stores (Cosmos, EF Core, FileSystem)
  // and making this in-memory store a faithful test double for them.
  private ulong NextVersionUnlocked => _events.Count == 0 ? _initialVersion : _events.Max(e => e.Version) + 1;

  public DateTimeOffset Created { get; }

  public ulong? LatestSnapshotVersion
  {
    get
    {
      lock (_lock)
      {
        return _events
          .Where(e => e.DocumentType == EventStreamDocumentType.Snapshot)
          .MaxBy(e => e.Version)?
          .Version;
      }
    }
  }

  public EventStreamMetaData MetaData { get; }

  public Task<EventStreamDocument?> GetLatestSnapshot(CancellationToken cancellationToken = default)
  {
    lock (_lock)
    {
      return Task.FromResult(_events.LastOrDefault(e => e.DocumentType == EventStreamDocumentType.Snapshot));
    }
  }

  public Task AppendAsync<TEvent>(Guid id, TEvent evt, EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default) where TEvent : notnull
  {
    string name = _typeProvider.ResolveType(typeof(TEvent));
    return AppendEntriesAsync(
      [new InMemoryAppendProto(id, name, JObject.FromObject(evt), EventStreamDocumentType.Event, metaData)],
      cancellationToken);
  }

  public Task AppendAsync(Guid id, string eventType, JObject evt, EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default)
    => AppendEntriesAsync(
      [new InMemoryAppendProto(id, eventType, evt, EventStreamDocumentType.Event, metaData)],
      cancellationToken);

  public Task AppendSnapshotAsync<TEntity>(Guid id, TEntity entity, EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default) where TEntity : notnull
    => AppendEntriesAsync(
      [new InMemoryAppendProto(id, _targetType, JObject.FromObject(entity), EventStreamDocumentType.Snapshot, metaData)],
      cancellationToken);

  /// <summary>
  /// Builds the documents, runs them through the append pipeline (which may attach
  /// signatures) and commits them under the append gate so the signature chain stays
  /// consistent. Used by both single and batch append.
  /// </summary>
  internal async Task AppendEntriesAsync(
    IReadOnlyList<InMemoryAppendProto> protos,
    CancellationToken cancellationToken)
  {
    if (protos.Count == 0)
    {
      return;
    }

    await _appendGate.WaitAsync(cancellationToken).ConfigureAwait(false);
    try
    {
      List<EventAppendEntry> entries;
      lock (_lock)
      {
        ulong next = NextVersionUnlocked;
        entries = protos
          .Select((p, i) => new EventAppendEntry(new EventStreamDocument
          {
            Id = p.Id,
            StreamId = StreamId,
            Version = next + (ulong)i,
            Time = _tp.GetLocalNow(),
            DataType = p.Name,
            Data = p.Data,
            DocumentType = p.Type,
            MetaData = p.MetaData ?? new EventStreamMetaData(),
            TargetType = _targetType,
            Name = p.Name,
          }))
          .ToList();
      }

      var context = new EventAppendContext
      {
        StreamId = StreamId,
        DocumentType = protos[0].Type,
        MetaData = protos[0].MetaData,
        PreviousSignature = _latestSignature,
        Entries = entries,
      };

      await _pipeline.ExecuteAsync(context, () =>
      {
        lock (_lock)
        {
          foreach (EventAppendEntry entry in context.Entries)
          {
            _events.Add(entry.Document);
          }
        }

        EventSignature? last = context.Entries[^1].Document.Signature;
        if (last is not null)
        {
          _latestSignature = last.Value;
        }

        return Task.CompletedTask;
      }, cancellationToken).ConfigureAwait(false);
    }
    finally
    {
      _appendGate.Release();
    }
  }

  public Task<IEventStoreTransactionAppender> CreateTransactionalBatchAsync()
  {
    return Task.FromResult<IEventStoreTransactionAppender>(
      new InMemoryTransactionalBatch(this, _typeProvider));
  }

  public IAsyncEnumerable<EventStreamDocument> ListAsync(ulong startVersion = 0,
    CancellationToken cancellationToken = default)
  {
    lock (_lock)
    {
      return _events.Where(evt => evt.Version >= startVersion).ToList().ToAsyncEnumerable();
    }
  }

  public IAsyncEnumerable<EventStreamDocument> ListAsync(ulong startVersion, ulong endVersion,
    CancellationToken cancellationToken = default)
  {
    lock (_lock)
    {
      return _events
        .Where(evt => evt.Version >= startVersion && evt.Version <= endVersion)
        .ToList()
        .ToAsyncEnumerable();
    }
  }

  public IAsyncEnumerable<EventStreamDocument> ListDescendingAsync(ulong endVersion, ulong startVersion,
    CancellationToken cancellationToken = default)
  {
    lock (_lock)
    {
      return _events.Where(evt => evt.Version >= startVersion && evt.Version <= endVersion)
        .OrderByDescending(evt => evt.Version)
        .ToList()
        .ToAsyncEnumerable();
    }
  }

  public IAsyncEnumerable<EventStreamDocument> ListDescendingAsync(ulong endVersion,
    CancellationToken cancellationToken = default)
  {
    lock (_lock)
    {
      return _events.Where(evt => evt.Version <= endVersion)
        .OrderByDescending(evt => evt.Version)
        .ToList()
        .ToAsyncEnumerable();
    }
  }

  public Task UpdateStreamMetaData(EventStreamMetaData metaData, CancellationToken cancellationToken = default)
  {
    return Task.CompletedTask;
  }
}

internal sealed class InMemoryTransactionalBatch(
  InMemoryEventStream stream,
  IEventTypeProvider typeProvider) : IEventStoreTransactionAppender
{
  private readonly List<InMemoryAppendProto> _protos = [];

  public IEventStoreTransactionAppender Add<TEvent>(Guid id, TEvent evt, EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default) where TEvent : notnull
  {
    string name = typeProvider.ResolveType(typeof(TEvent));
    _protos.Add(new InMemoryAppendProto(id, name, JObject.FromObject(evt), EventStreamDocumentType.Event, metaData));
    return this;
  }

  public Task CommitAsync(CancellationToken cancellationToken = default)
    => stream.AppendEntriesAsync(_protos, cancellationToken);
}
