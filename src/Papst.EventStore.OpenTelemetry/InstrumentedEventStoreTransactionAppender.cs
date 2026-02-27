using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Papst.EventStore.Documents;

namespace Papst.EventStore.OpenTelemetry;

/// <summary>
/// A decorator for <see cref="IEventStoreTransactionAppender"/> that instruments all operations with OpenTelemetry activities.
/// </summary>
internal sealed class InstrumentedEventStoreTransactionAppender : IEventStoreTransactionAppender
{
  private readonly IEventStoreTransactionAppender _inner;
  private readonly Guid _streamId;

  public InstrumentedEventStoreTransactionAppender(IEventStoreTransactionAppender inner, Guid streamId)
  {
    _inner = inner;
    _streamId = streamId;
  }

  /// <inheritdoc />
  public IEventStoreTransactionAppender Add<TEvent>(Guid id, TEvent evt, EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default) where TEvent : notnull
  {
    using Activity? activity = EventStoreActivitySource.Source.StartActivity("EventStore.TransactionAppender.Add");
    activity?.SetTag("event_store.stream_id", _streamId.ToString());
    activity?.SetTag("event_store.event_id", id.ToString());
    activity?.SetTag("event_store.event_type", typeof(TEvent).FullName ?? typeof(TEvent).Name);

    _inner.Add(id, evt, metaData, cancellationToken);
    return this;
  }

  /// <inheritdoc />
  public async Task CommitAsync(CancellationToken cancellationToken = default)
  {
    using Activity? activity = EventStoreActivitySource.Source.StartActivity("EventStore.TransactionAppender.Commit");
    activity?.SetTag("event_store.stream_id", _streamId.ToString());

    try
    {
      await _inner.CommitAsync(cancellationToken).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      throw;
    }
  }
}
