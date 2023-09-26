using Papst.EventStore.Documents;

namespace Papst.EventStore.AzureCosmos;

/// <summary>
/// The <see cref="ICosmosIdStrategy"/> controls how ids for the cosmos entities are generated
/// </summary>
public interface ICosmosIdStrategy
{
  /// <summary>
  /// Generate an id string for the given properties of an <see cref="EventStreamDocument"/>
  /// </summary>
  /// <param name="id"></param>
  /// <param name="streamId"></param>
  /// <param name="version"></param>
  /// <param name="type"></param>
  /// <returns></returns>
  ValueTask<string> GenerateId(Guid id, Guid streamId, ulong version, EventStreamDocumentType type);
}
