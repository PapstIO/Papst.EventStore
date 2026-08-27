namespace Papst.EventStore.Documents;

/// <summary>
/// Cryptographic signature of a single <see cref="EventStreamDocument"/>.
/// The signature covers a canonical representation of the Event data and, when
/// chaining is enabled, the <see cref="PreviousSignature"/> of the preceding
/// document, making the stream tamper evident.
/// </summary>
public record EventSignature
{
  /// <summary>
  /// The signing algorithm identifier, e.g. <c>RS256</c> or <c>ES256</c>.
  /// </summary>
  public string Algorithm { get; init; } = string.Empty;

  /// <summary>
  /// Thumbprint of the X.509 certificate whose private key produced the signature.
  /// Used by a verifier to resolve the historical certificate.
  /// </summary>
  public string CertificateThumbprint { get; init; } = string.Empty;

  /// <summary>
  /// Base64 encoded hash of the canonical event payload that has been signed.
  /// </summary>
  public string Hash { get; init; } = string.Empty;

  /// <summary>
  /// Base64 encoded signature bytes.
  /// </summary>
  public string Value { get; init; } = string.Empty;

  /// <summary>
  /// The <see cref="Value"/> of the previous document's signature that this
  /// signature chains onto, or <see langword="null"/> for the first document in
  /// a stream (or when chaining is disabled).
  /// </summary>
  public string? PreviousSignature { get; init; }
}
