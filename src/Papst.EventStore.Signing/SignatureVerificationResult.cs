using System.Collections.Generic;

namespace Papst.EventStore.Signing;

/// <summary>
/// A single verification failure for a document version.
/// </summary>
/// <param name="Version">The document version that failed.</param>
/// <param name="Reason">A human readable reason.</param>
public record SignatureVerificationFailure(ulong Version, string Reason);

/// <summary>
/// The outcome of verifying a stream's (or a document's) signatures.
/// </summary>
public record SignatureVerificationResult
{
  /// <summary>
  /// True when every verified document had a valid signature and an intact chain.
  /// </summary>
  public bool IsValid { get; init; }

  /// <summary>
  /// The version of the first document that failed verification, if any.
  /// </summary>
  public ulong? FirstInvalidVersion { get; init; }

  /// <summary>
  /// All verification failures, in ascending version order.
  /// </summary>
  public IReadOnlyList<SignatureVerificationFailure> Failures { get; init; } = [];

  internal static SignatureVerificationResult Valid { get; } = new() { IsValid = true };

  internal static SignatureVerificationResult FromFailures(IReadOnlyList<SignatureVerificationFailure> failures)
    => failures.Count == 0
      ? Valid
      : new SignatureVerificationResult
      {
        IsValid = false,
        FirstInvalidVersion = failures[0].Version,
        Failures = failures,
      };
}
