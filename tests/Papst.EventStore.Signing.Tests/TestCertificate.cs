using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Papst.EventStore.Signing.Tests;

internal static class TestCertificate
{
  public static X509Certificate2 CreateRsa()
  {
    using var rsa = RSA.Create(2048);
    var request = new CertificateRequest("CN=Papst.EventStore.Signing.Tests", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
  }

  public static X509Certificate2 CreateEcdsa()
  {
    using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    var request = new CertificateRequest("CN=Papst.EventStore.Signing.Tests.Ec", ecdsa, HashAlgorithmName.SHA256);
    return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
  }
}
