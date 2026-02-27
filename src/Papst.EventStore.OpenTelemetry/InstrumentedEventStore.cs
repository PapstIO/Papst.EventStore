using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Papst.EventStore.OpenTelemetry;

/// <summary>
/// A decorator for <see cref="IEventStore"/> that instruments all operations with OpenTelemetry activities.
/// </summary>
internal sealed class InstrumentedEventStore : IEventStore
{
  private readonly IEventStore _inner;

  public InstrumentedEventStore(IEventStore inner)
  {
    _inner = inner;
  }

  /// <inheritdoc />
  public async Task<IEventStream> GetAsync(Guid streamId, CancellationToken cancellationToken = default)
  {
    using Activity? activity = EventStoreActivitySource.Source.StartActivity("EventStore.Get");
    activity?.SetTag("event_store.stream_id", streamId.ToString());

    try
    {
      IEventStream stream = await _inner.GetAsync(streamId, cancellationToken).ConfigureAwait(false);
      activity?.SetTag("event_store.stream_version", stream.Version.ToString());
      return new InstrumentedEventStream(stream);
    }
    catch (Exception ex)
    {
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      throw;
    }
  }

  /// <inheritdoc />
  public async Task<IEventStream> CreateAsync(Guid streamId, string targetTypeName,
    CancellationToken cancellationToken = default)
  {
    using Activity? activity = EventStoreActivitySource.Source.StartActivity("EventStore.Create");
    activity?.SetTag("event_store.stream_id", streamId.ToString());
    activity?.SetTag("event_store.target_type", targetTypeName);

    try
    {
      IEventStream stream = await _inner.CreateAsync(streamId, targetTypeName, cancellationToken)
        .ConfigureAwait(false);
      return new InstrumentedEventStream(stream);
    }
    catch (Exception ex)
    {
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      throw;
    }
  }

  /// <inheritdoc />
  public async Task<IEventStream> CreateAsync(Guid streamId, string targetTypeName, string? tenantId,
    string? userId, string? username, string? comment,
    Dictionary<string, string>? additionalMetaData, CancellationToken cancellationToken = default)
  {
    using Activity? activity = EventStoreActivitySource.Source.StartActivity("EventStore.Create");
    activity?.SetTag("event_store.stream_id", streamId.ToString());
    activity?.SetTag("event_store.target_type", targetTypeName);
    if (tenantId != null)
    {
      activity?.SetTag("event_store.tenant_id", tenantId);
    }

    try
    {
      IEventStream stream = await _inner.CreateAsync(streamId, targetTypeName, tenantId, userId, username,
        comment, additionalMetaData, cancellationToken).ConfigureAwait(false);
      return new InstrumentedEventStream(stream);
    }
    catch (Exception ex)
    {
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      throw;
    }
  }
}
