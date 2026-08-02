using System;
using Microsoft.Extensions.Logging;

namespace Papst.EventStore.InMemory;

internal static partial class Logging
{
  [LoggerMessage(LogLevel.Information, "Deleting EventStream with Id {StreamId}")]
  public static partial void DeletingEventStream(ILogger logger, Guid streamId);

  [LoggerMessage(LogLevel.Information, "Deleted EventStream with Id {StreamId}")]
  public static partial void DeletedEventStream(ILogger logger, Guid streamId);
}
