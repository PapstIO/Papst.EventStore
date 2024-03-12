﻿using Microsoft.Extensions.Logging;

namespace Papst.EventStore.AzureCosmos;
internal static partial class Logging
{
  [LoggerMessage(EventId = 100_100, EventName = nameof(CreatingEventStream), Level = LogLevel.Information, Message = "Creating new EventStream with Id {StreamId} for Entity {TargetType}")]
  public static partial void CreatingEventStream(ILogger logger, Guid streamId, string targetType);

  [LoggerMessage(EventId = 100_101, EventName = nameof(GetEventStream), Level = LogLevel.Debug, Message = "Retrieving EventStream with Id {StreamId}")]
  public static partial void GetEventStream(ILogger logger, Guid streamId);

  [LoggerMessage(EventId = 100_102, EventName = nameof(AppendingEvent), Level = LogLevel.Information, Message = "Appending Event {EventName} to Stream {StreamId} with Version {Version}")]
  public static partial void AppendingEvent(ILogger logger, string eventName, Guid streamId, ulong version);

  [LoggerMessage(EventId = 100_103, EventName = nameof(ReadingEventStream), Level = LogLevel.Information, Message = "Reading EventStream {StreamId} from Version {StartVersion} to {EndVersion}")]
  public static partial void ReadingEventStream(ILogger logger, Guid streamId, ulong startVersion, ulong endVersion);

  [LoggerMessage(EventId = 100_104, EventName = nameof(ReadingEvent), Level = LogLevel.Debug, Message = "Reading Event {Version} for Stream {StreamId}")]
  public static partial void ReadingEvent(ILogger logger, Guid streamId, ulong version);

  [LoggerMessage(EventId = 100_105, EventName = nameof(IndexPatchConcurrency), Level = LogLevel.Warning, Message = "Failed to Update index due to concurrency - retrying")]
  public static partial void IndexPatchConcurrency(ILogger logger, Exception ex, Guid streamId);

}
