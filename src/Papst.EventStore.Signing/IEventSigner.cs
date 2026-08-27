using System.Threading;
using System.Threading.Tasks;
using Papst.EventStore.Documents;

namespace Papst.EventStore.Signing;

/// <summary>
/// Produces the cryptographic signature for an event stream document.
/// </summary>
public interface IEventSigner
{
  /// <summary>
  /// Signs <paramref name="document"/>, chaining onto <paramref name="previousSignature"/>
  /// when chaining is enabled. Asynchronous so the signing certificate can be
  /// resolved from an async source (e.g. a Key Vault) on first use.
  /// </summary>
  /// <param name="document">The document to sign.</param>
  /// <param name="previousSignature">
  /// The <see cref="EventSignature.Value"/> of the chain head, or <see langword="null"/>
  /// for the first signed document in the stream.
  /// </param>
  /// <param name="cancellationToken"></param>
  /// <returns>The signature to store on the document.</returns>
  ValueTask<EventSignature> SignAsync(
    EventStreamDocument document,
    string? previousSignature,
    CancellationToken cancellationToken = default);
}
