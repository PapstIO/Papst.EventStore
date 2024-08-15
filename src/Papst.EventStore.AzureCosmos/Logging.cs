using Microsoft.Extensions.Logging;

namespace Papst.EventStore.AzureCosmos;
internal static partial class Logging
{
  [LoggerMessage(LogLevel.Information, "Creating new EventStream with Id {StreamId} for Entity {TargetType}")]
  public static partial void CreatingEventStream(this ILogger logger, Guid streamId, string targetType);

  [LoggerMessage(LogLevel.Debug, "Retrieving EventStream with Id {StreamId}")]
  public static partial void GetEventStream(this ILogger logger, Guid streamId);

  [LoggerMessage(LogLevel.Information, "Appending Event {EventName} to Stream {StreamId} with Version {Version}")]
  public static partial void AppendingEvent(this ILogger logger, string eventName, Guid streamId, ulong version);

  [LoggerMessage(LogLevel.Information, "Reading EventStream {StreamId} from Version {StartVersion} to {EndVersion}")]
  public static partial void ReadingEventStream(this ILogger logger, Guid streamId, ulong startVersion, ulong endVersion);

  [LoggerMessage(LogLevel.Debug, "Reading Event {Version} for Stream {StreamId}")]
  public static partial void ReadingEvent(this ILogger logger, Guid streamId, ulong version);

  [LoggerMessage(LogLevel.Warning, "Failed to Update index of Stream {StreamId} due to concurrency - retrying")]
  public static partial void IndexPatchConcurrency(this ILogger logger, Exception ex, Guid streamId);

  [LoggerMessage(LogLevel.Warning, "Exception during Transaction Commit of Stream {StreamId}")]
  public static partial void TransactionException(this ILogger logger, Exception ex, Guid streamId);
}
