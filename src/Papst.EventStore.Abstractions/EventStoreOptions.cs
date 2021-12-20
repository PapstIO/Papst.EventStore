namespace Papst.EventStore.Abstractions;

public class EventStoreOptions
{
  /// <summary>
  /// The Start Version for new Streams
  /// Defaults to 0
  /// </summary>
  public ulong StartVersion { get; set; } = 0;
}
