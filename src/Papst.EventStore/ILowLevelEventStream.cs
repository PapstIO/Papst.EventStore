using Newtonsoft.Json.Linq;
using Papst.EventStore.Documents;

namespace Papst.EventStore;

public interface ILowLevelEventStream
{
  /// <summary>
  /// Appends an Low Level Event to the Stream.
  /// The Event is encoded as a JObject and the Event Type is provided as a string. This allows to store Events that are not known at compile time.
  /// </summary>
  /// <param name="id"></param>
  /// <param name="eventType"></param>
  /// <param name="evt"></param>
  /// <param name="metaData"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  Task AppendAsync(
    Guid id,
    string eventType,
    JObject evt,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default
  );
}
