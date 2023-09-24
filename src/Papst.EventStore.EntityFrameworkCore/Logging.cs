using Microsoft.Extensions.Logging;

namespace Papst.EventStore.EntityFrameworkCore;
internal static partial class Logging
{
  private const string EventName = "Papst.EventStore.FileSystem";

  [LoggerMessage(EventId = 100_100, EventName = EventName, Level = LogLevel.Information, Message = "Creating new EventStream with Id {StreamId} for Entity {TargetType}")]
  public static partial void CreatingEventStream(ILogger logger, Guid streamId, string targetType);

  [LoggerMessage(EventId = 100_101, EventName = EventName, Level = LogLevel.Debug, Message = "Retrieving EventStream with Id {StreamId}")]
  public static partial void GetEventStream(ILogger logger, Guid streamId);

  [LoggerMessage(EventId = 100_102, EventName = EventName, Level = LogLevel.Information, Message = "Appending Event {EventName} to Stream {StreamId} with Version {Version}")]
  public static partial void AppendingEvent(ILogger logger, string eventName, Guid streamId, ulong version);

  [LoggerMessage(EventId = 100_103, EventName = EventName, Level = LogLevel.Information, Message = "Reading EventStream {StreamId} from Version {StartVersion} to {EndVersion}")]
  public static partial void ReadingEventStream(ILogger logger, Guid streamId, ulong startVersion, ulong endVersion);

  [LoggerMessage(EventId = 100_104, EventName = EventName, Level = LogLevel.Debug, Message = "Reading Event {Version} for Stream {StreamId}")]
  public static partial void ReadingEvent(ILogger logger, Guid streamId, ulong version);

}
