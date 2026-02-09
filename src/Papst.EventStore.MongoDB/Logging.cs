using System;
using Microsoft.Extensions.Logging;

namespace Papst.EventStore.MongoDB;

internal static partial class Logging
{
  [LoggerMessage(LogLevel.Information, "Creating new EventStream with Id {StreamId} for Entity {TargetType}")]
  public static partial void CreatingEventStream(this ILogger logger, Guid streamId, string targetType);

  [LoggerMessage(LogLevel.Debug, "Retrieving EventStream with Id {StreamId}")]
  public static partial void GetEventStream(this ILogger logger, Guid streamId);

  [LoggerMessage(LogLevel.Information, "Appending Event {EventName} to Stream {StreamId} with Version {Version}")]
  public static partial void AppendingEvent(this ILogger logger, string eventName, Guid streamId, ulong version);

  [LoggerMessage(LogLevel.Information, "Appending Snapshot to Stream {StreamId} with Version {Version}")]
  public static partial void AppendingSnapshot(this ILogger logger, Guid streamId, ulong version);

  [LoggerMessage(LogLevel.Debug, "Reading EventStream {StreamId} from Version {StartVersion}")]
  public static partial void ReadingEventStream(this ILogger logger, Guid streamId, ulong startVersion);

  [LoggerMessage(LogLevel.Warning, "MongoDB standalone detected - transactions not supported, falling back to non-transactional batch operation for Stream {StreamId}")]
  public static partial void TransactionNotSupported(this ILogger logger, Guid streamId);

  [LoggerMessage(LogLevel.Information, "Successfully committed transaction batch to Stream {StreamId} with {Count} events")]
  public static partial void TransactionCompleted(this ILogger logger, Guid streamId, int count);

  [LoggerMessage(LogLevel.Warning, "Exception during transaction commit for Stream {StreamId}")]
  public static partial void TransactionException(this ILogger logger, Exception ex, Guid streamId);

  [LoggerMessage(LogLevel.Information, "Ensuring MongoDB indexes are created")]
  public static partial void EnsuringIndexes(this ILogger logger);

  [LoggerMessage(LogLevel.Information, "MongoDB indexes created successfully")]
  public static partial void IndexesCreated(this ILogger logger);
}
