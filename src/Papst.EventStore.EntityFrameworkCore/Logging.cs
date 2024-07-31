using Microsoft.Extensions.Logging;

namespace Papst.EventStore.EntityFrameworkCore;
internal static partial class Logging
{
  [LoggerMessage(LogLevel.Information, "Creating new EventStream with Id {StreamId} for Entity {TargetType}")]
  public static partial void CreatingEventStream(ILogger logger, Guid streamId, string targetType);

  [LoggerMessage(LogLevel.Debug, "Retrieving EventStream with Id {StreamId}")]
  public static partial void GetEventStream(ILogger logger, Guid streamId);

  [LoggerMessage(LogLevel.Information, "Appending Event {EventName} to Stream {StreamId} with Version {Version}")]
  public static partial void AppendingEvent(ILogger logger, string eventName, Guid streamId, ulong version);

  [LoggerMessage(LogLevel.Information, "Reading EventStream {StreamId} from Version {StartVersion} to {EndVersion}")]
  public static partial void ReadingEventStream(ILogger logger, Guid streamId, ulong startVersion, ulong endVersion);

  [LoggerMessage(LogLevel.Debug, "Reading Event {Version} for Stream {StreamId}")]
  public static partial void ReadingEvent(ILogger logger, Guid streamId, ulong version);
}
