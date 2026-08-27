using System.Threading;
using System.Threading.Tasks;
using Papst.EventStore;
using Papst.EventStore.Documents;

namespace Papst.EventStore.Signing;

/// <summary>
/// Verifies the signatures (and, when chained, the signature chain) of a stream or
/// a single document.
/// </summary>
public interface IEventStreamSignatureVerifier
{
  /// <summary>
  /// Verifies every document of <paramref name="stream"/> in ascending version order.
  /// </summary>
  Task<SignatureVerificationResult> VerifyAsync(IEventStream stream, CancellationToken cancellationToken = default);

  /// <summary>
  /// Verifies a single <paramref name="document"/> without checking chain linkage.
  /// </summary>
  SignatureVerificationResult Verify(EventStreamDocument document);
}
