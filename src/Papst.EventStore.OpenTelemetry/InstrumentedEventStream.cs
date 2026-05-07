using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Papst.EventStore.Documents;

namespace Papst.EventStore.OpenTelemetry;

/// <summary>
/// A decorator for <see cref="IEventStream"/> that instruments all operations with OpenTelemetry activities.
/// </summary>
internal sealed class InstrumentedEventStream : IEventStream
{
  private readonly IEventStream _inner;

  public InstrumentedEventStream(IEventStream inner)
  {
    _inner = inner;
  }

  /// <inheritdoc />
  public Guid StreamId => _inner.StreamId;

  /// <inheritdoc />
  public ulong Version => _inner.Version;

  /// <inheritdoc />
  public DateTimeOffset Created => _inner.Created;

  /// <inheritdoc />
  public ulong? LatestSnapshotVersion => _inner.LatestSnapshotVersion;

  /// <inheritdoc />
  public EventStreamMetaData MetaData => _inner.MetaData;

  /// <inheritdoc />
  public Task<EventStreamDocument?> GetLatestSnapshot(CancellationToken cancellationToken = default)
  {
    using Activity? activity = EventStoreActivitySource.Source.StartActivity("EventStream.GetLatestSnapshot");
    activity?.SetTag("event_store.stream_id", StreamId.ToString());
    return _inner.GetLatestSnapshot(cancellationToken);
  }

  /// <inheritdoc />
  public async Task AppendAsync<TEvent>(Guid id, TEvent evt, EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default) where TEvent : notnull
  {
    using Activity? activity = EventStoreActivitySource.Source.StartActivity("EventStream.Append");
    activity?.SetTag("event_store.stream_id", StreamId.ToString());
    activity?.SetTag("event_store.event_id", id.ToString());
    activity?.SetTag("event_store.event_type", typeof(TEvent).FullName ?? typeof(TEvent).Name);

    try
    {
      await _inner.AppendAsync(id, evt, metaData, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      throw;
    }
  }

  /// <inheritdoc />
  public async Task AppendSnapshotAsync<TEntity>(Guid id, TEntity entity, EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default) where TEntity : notnull
  {
    using Activity? activity = EventStoreActivitySource.Source.StartActivity("EventStream.AppendSnapshot");
    activity?.SetTag("event_store.stream_id", StreamId.ToString());
    activity?.SetTag("event_store.event_id", id.ToString());
    activity?.SetTag("event_store.entity_type", typeof(TEntity).FullName ?? typeof(TEntity).Name);

    try
    {
      await _inner.AppendSnapshotAsync(id, entity, metaData, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      throw;
    }
  }

  /// <inheritdoc />
  public async Task<IEventStoreTransactionAppender> CreateTransactionalBatchAsync()
  {
    using Activity? activity = EventStoreActivitySource.Source.StartActivity("EventStream.CreateTransactionalBatch");
    activity?.SetTag("event_store.stream_id", StreamId.ToString());

    try
    {
      IEventStoreTransactionAppender appender = await _inner.CreateTransactionalBatchAsync().ConfigureAwait(false);
      return new InstrumentedEventStoreTransactionAppender(appender, StreamId);
    }
    catch (Exception ex)
    {
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      throw;
    }
  }

  /// <inheritdoc />
  public IAsyncEnumerable<EventStreamDocument> ListAsync(ulong startVersion = 0,
    CancellationToken cancellationToken = default)
    => ListAsyncCore(_inner.ListAsync(startVersion, cancellationToken), "EventStream.List", cancellationToken);

  /// <inheritdoc />
  public IAsyncEnumerable<EventStreamDocument> ListAsync(ulong startVersion, ulong endVersion,
    CancellationToken cancellationToken = default)
    => ListAsyncCore(_inner.ListAsync(startVersion, endVersion, cancellationToken), "EventStream.List", cancellationToken);

  /// <inheritdoc />
  public IAsyncEnumerable<EventStreamDocument> ListDescendingAsync(ulong endVersion, ulong startVersion,
    CancellationToken cancellationToken = default)
    => ListAsyncCore(_inner.ListDescendingAsync(endVersion, startVersion, cancellationToken), "EventStream.ListDescending", cancellationToken);

  /// <inheritdoc />
  public IAsyncEnumerable<EventStreamDocument> ListDescendingAsync(ulong endVersion,
    CancellationToken cancellationToken = default)
    => ListAsyncCore(_inner.ListDescendingAsync(endVersion, cancellationToken), "EventStream.ListDescending", cancellationToken);

  /// <inheritdoc />
  public Task UpdateStreamMetaData(EventStreamMetaData metaData, CancellationToken cancellationToken = default)
  {
    using Activity? activity = EventStoreActivitySource.Source.StartActivity("EventStream.UpdateMetaData");
    activity?.SetTag("event_store.stream_id", StreamId.ToString());
    return _inner.UpdateStreamMetaData(metaData, cancellationToken);
  }

  private async IAsyncEnumerable<EventStreamDocument> ListAsyncCore(
    IAsyncEnumerable<EventStreamDocument> source,
    string activityName,
    [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    using Activity? activity = EventStoreActivitySource.Source.StartActivity(activityName);
    activity?.SetTag("event_store.stream_id", StreamId.ToString());

    ulong count = 0;
    await foreach (EventStreamDocument doc in source.WithCancellation(cancellationToken).ConfigureAwait(false))
    {
      count++;
      yield return doc;
    }

    activity?.SetTag("event_store.document_count", count.ToString());
  }
}
