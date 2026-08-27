using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Papst.EventStore.Signing;

/// <summary>
/// Options for event signing. Supply the signing certificate through
/// <see cref="Certificate"/>, <see cref="CertificateFactory"/> or
/// <see cref="CertificateFactoryAsync"/> — the private key comes from the host,
/// never from configuration.
/// </summary>
public class EventSigningOptions
{
  /// <summary>
  /// The signing certificate (must contain a private key). Alternatively use
  /// <see cref="CertificateFactory"/> or <see cref="CertificateFactoryAsync"/>.
  /// </summary>
  public X509Certificate2? Certificate { get; set; }

  /// <summary>
  /// Resolves the signing certificate from the service provider. Used when the
  /// certificate is provided by another service.
  /// </summary>
  public Func<IServiceProvider, X509Certificate2>? CertificateFactory { get; set; }

  /// <summary>
  /// Asynchronously resolves the signing certificate from the service provider,
  /// e.g. when it must be fetched from a Key Vault.
  /// </summary>
  public Func<IServiceProvider, CancellationToken, ValueTask<X509Certificate2>>? CertificateFactoryAsync { get; set; }

  /// <summary>
  /// Resolves a historical certificate by its thumbprint for verification, so
  /// signatures produced by a rotated certificate remain verifiable. When
  /// <see langword="null"/>, verification falls back to the configured certificate
  /// and requires the stored thumbprint to match it.
  /// </summary>
  public Func<string, X509Certificate2?>? CertificateResolver { get; set; }

  /// <summary>
  /// Asynchronously resolves a historical certificate by its thumbprint for
  /// verification. Preferred over <see cref="CertificateResolver"/> when set.
  /// </summary>
  public Func<string, CancellationToken, ValueTask<X509Certificate2?>>? CertificateResolverAsync { get; set; }

  /// <summary>
  /// JWS-style signature algorithm identifier. Supported: <c>RS256/384/512</c>,
  /// <c>PS256/384/512</c>, <c>ES256/384/512</c>. Default <c>RS256</c>.
  /// </summary>
  public string Algorithm { get; set; } = "RS256";

  /// <summary>
  /// When <see langword="true"/> (default) each signature chains onto the previous
  /// document's signature, making deletion, reordering and truncation detectable.
  /// </summary>
  public bool EnableChaining { get; set; } = true;

  /// <summary>
  /// When <see langword="true"/> every document is verified as it is read and an
  /// <see cref="EventStreamSignatureException"/> is thrown on mismatch. Default
  /// <see langword="false"/>; the explicit
  /// <see cref="IEventStreamSignatureVerifier"/> is the primary path.
  /// </summary>
  public bool VerifyOnRead { get; set; }

  internal async ValueTask<X509Certificate2> ResolveSigningCertificateAsync(IServiceProvider services, CancellationToken cancellationToken)
  {
    if (Certificate is not null)
    {
      return Certificate;
    }

    if (CertificateFactory is not null)
    {
      return CertificateFactory(services);
    }

    if (CertificateFactoryAsync is not null)
    {
      return await CertificateFactoryAsync(services, cancellationToken).ConfigureAwait(false);
    }

    throw new InvalidOperationException(
      "No signing certificate configured. Set EventSigningOptions.Certificate, CertificateFactory or CertificateFactoryAsync.");
  }

  /// <summary>
  /// Resolves a certificate for verification by thumbprint, using the async and
  /// sync resolvers, then the configured certificate when its thumbprint matches.
  /// </summary>
  internal async ValueTask<X509Certificate2?> ResolveVerificationCertificateAsync(
    IServiceProvider services,
    string thumbprint,
    CancellationToken cancellationToken)
  {
    if (CertificateResolverAsync is not null)
    {
      X509Certificate2? resolved = await CertificateResolverAsync(thumbprint, cancellationToken).ConfigureAwait(false);
      if (resolved is not null)
      {
        return resolved;
      }
    }

    return ResolveVerificationCertificate(services, thumbprint, allowFactory: true);
  }

  /// <summary>
  /// Synchronous verification certificate resolution. Cannot use the async
  /// resolver or async factory.
  /// </summary>
  internal X509Certificate2? ResolveVerificationCertificate(IServiceProvider services, string thumbprint)
    => ResolveVerificationCertificate(services, thumbprint, allowFactory: true);

  private X509Certificate2? ResolveVerificationCertificate(IServiceProvider services, string thumbprint, bool allowFactory)
  {
    if (CertificateResolver is not null)
    {
      X509Certificate2? resolved = CertificateResolver(thumbprint);
      if (resolved is not null)
      {
        return resolved;
      }
    }

    X509Certificate2? configured = Certificate ?? (allowFactory ? CertificateFactory?.Invoke(services) : null);
    return configured is not null
           && string.Equals(configured.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase)
      ? configured
      : null;
  }
}
