using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Papst.EventStore.Documents;

namespace Papst.EventStore.Signing;

/// <summary>
/// <see cref="IEventSigner"/> backed by an X.509 certificate's private key. The
/// certificate is resolved once, on first use, and then cached.
/// </summary>
internal sealed class X509EventSigner : IEventSigner
{
  private readonly EventSigningOptions _options;
  private readonly IServiceProvider _services;
  private readonly SemaphoreSlim _certificateGate = new(1, 1);
  private X509Certificate2? _certificate;

  public X509EventSigner(IOptions<EventSigningOptions> options, IServiceProvider services)
  {
    _options = options.Value;
    _services = services;
  }

  public async ValueTask<EventSignature> SignAsync(
    EventStreamDocument document,
    string? previousSignature,
    CancellationToken cancellationToken = default)
  {
    string? chainHead = _options.EnableChaining ? previousSignature : null;
    X509Certificate2 certificate = await GetCertificateAsync(cancellationToken).ConfigureAwait(false);

    (string hash, byte[] message) = SigningPayload.Build(document, _options.Algorithm, chainHead);
    byte[] signature = SigningAlgorithm.Sign(_options.Algorithm, certificate, message);

    return new EventSignature
    {
      Algorithm = _options.Algorithm,
      CertificateThumbprint = certificate.Thumbprint,
      Hash = hash,
      Value = Convert.ToBase64String(signature),
      PreviousSignature = chainHead,
    };
  }

  private async ValueTask<X509Certificate2> GetCertificateAsync(CancellationToken cancellationToken)
  {
    if (_certificate is not null)
    {
      return _certificate;
    }

    await _certificateGate.WaitAsync(cancellationToken).ConfigureAwait(false);
    try
    {
      return _certificate ??= await _options.ResolveSigningCertificateAsync(_services, cancellationToken).ConfigureAwait(false);
    }
    finally
    {
      _certificateGate.Release();
    }
  }
}
