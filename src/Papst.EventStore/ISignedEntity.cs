using Papst.EventStore.Documents;

namespace Papst.EventStore;

/// <summary>
/// Code Contract for an Entity that carries the signature of the Event Stream
/// Document it has been aggregated up to.
/// </summary>
public interface ISignedEntity : IEntity
{
  /// <summary>
  /// Signature of the current <see cref="IEntity.Version"/> of the Entity.
  /// The property is updated by the aggregator with the signature of the latest
  /// applied Event Stream Document.
  /// </summary>
  EventSignature? Signature { get; set; }
}
