using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Papst.EventStore;
using Papst.EventStore.Documents;

namespace Papst.EventStore.Signing;

/// <summary>
/// Verifies signatures against the configured certificate (or a historical
/// certificate resolved by thumbprint) and, for streams, the chain linkage.
/// </summary>
internal sealed class X509SignatureVerifier : IEventStreamSignatureVerifier
{
  private readonly EventSigningOptions _options;
  private readonly IServiceProvider _services;

  public X509SignatureVerifier(IOptions<EventSigningOptions> options, IServiceProvider services)
  {
    _options = options.Value;
    _services = services;
  }

  public async Task<SignatureVerificationResult> VerifyAsync(IEventStream stream, CancellationToken cancellationToken = default)
  {
    var failures = new List<SignatureVerificationFailure>();
    string? previous = null;

    await foreach (EventStreamDocument document in stream.ListAsync(0, cancellationToken).ConfigureAwait(false))
    {
      string? preValidation = ValidateStructure(document, expectedPrevious: previous, checkChain: _options.EnableChaining);
      if (preValidation is not null)
      {
        failures.Add(new SignatureVerificationFailure(document.Version, preValidation));
      }
      else
      {
        X509Certificate2? certificate = await _options
          .ResolveVerificationCertificateAsync(_services, document.Signature!.CertificateThumbprint, cancellationToken)
          .ConfigureAwait(false);

        string? reason = VerifyCryptographically(document, certificate);
        if (reason is not null)
        {
          failures.Add(new SignatureVerificationFailure(document.Version, reason));
        }
      }

      previous = document.Signature?.Value;
    }

    return SignatureVerificationResult.FromFailures(failures);
  }

  public SignatureVerificationResult Verify(EventStreamDocument document)
  {
    string? preValidation = ValidateStructure(document, expectedPrevious: null, checkChain: false);
    if (preValidation is not null)
    {
      return SignatureVerificationResult.FromFailures([new SignatureVerificationFailure(document.Version, preValidation)]);
    }

    X509Certificate2? certificate = _options.ResolveVerificationCertificate(_services, document.Signature!.CertificateThumbprint);
    string? reason = VerifyCryptographically(document, certificate);
    return reason is null
      ? SignatureVerificationResult.Valid
      : SignatureVerificationResult.FromFailures([new SignatureVerificationFailure(document.Version, reason)]);
  }

  /// <summary>Checks presence and chain linkage. Returns a reason, or null when structurally sound.</summary>
  private static string? ValidateStructure(EventStreamDocument document, string? expectedPrevious, bool checkChain)
  {
    EventSignature? signature = document.Signature;
    if (signature is null)
    {
      return "Document has no signature.";
    }

    if (checkChain && signature.PreviousSignature != expectedPrevious)
    {
      return "Signature chain is broken: previous signature does not match the preceding document.";
    }

    return null;
  }

  /// <summary>Verifies the hash and signature bytes. Returns a reason, or null when valid.</summary>
  private static string? VerifyCryptographically(EventStreamDocument document, X509Certificate2? certificate)
  {
    EventSignature signature = document.Signature!;
    if (certificate is null)
    {
      return $"No certificate available for thumbprint '{signature.CertificateThumbprint}'.";
    }

    (string hash, byte[] message) = SigningPayload.Build(document, signature.Algorithm, signature.PreviousSignature);
    if (hash != signature.Hash)
    {
      return "Payload hash does not match the stored signature hash (data was modified).";
    }

    byte[] signatureBytes;
    try
    {
      signatureBytes = Convert.FromBase64String(signature.Value);
    }
    catch (FormatException)
    {
      return "Signature value is not valid base64.";
    }

    return SigningAlgorithm.Verify(signature.Algorithm, certificate, message, signatureBytes)
      ? null
      : "Signature verification failed.";
  }
}
