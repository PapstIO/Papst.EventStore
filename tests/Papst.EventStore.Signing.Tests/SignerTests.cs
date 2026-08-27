using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Papst.EventStore.Documents;
using Shouldly;

namespace Papst.EventStore.Signing.Tests;

public class SignerTests
{
  private static (IEventSigner Signer, IEventStreamSignatureVerifier Verifier) Build(string algorithm)
  {
    X509Certificate2 cert = algorithm.StartsWith("ES") ? TestCertificate.CreateEcdsa() : TestCertificate.CreateRsa();
    var services = new ServiceCollection();
    services.AddEventStoreSigning(o =>
    {
      o.Certificate = cert;
      o.Algorithm = algorithm;
    });
    var provider = services.BuildServiceProvider();
    return (provider.GetRequiredService<IEventSigner>(), provider.GetRequiredService<IEventStreamSignatureVerifier>());
  }

  private static EventStreamDocument Doc(JObject data, ulong version = 0)
    => new()
    {
      Id = Guid.NewGuid(),
      StreamId = Guid.Empty,
      Version = version,
      DataType = "Test",
      Name = "Test",
      DocumentType = EventStreamDocumentType.Event,
      Data = data,
    };

  [Fact]
  public async Task CanonicalJson_IsPropertyOrderIndependent()
  {
    var (signer, _) = Build("RS256");

    var a = Doc(new JObject { ["b"] = 2, ["a"] = 1 });
    var b = Doc(new JObject { ["a"] = 1, ["b"] = 2 });

    (await signer.SignAsync(a, null, TestContext.Current.CancellationToken)).Hash.ShouldBe((await signer.SignAsync(b, null, TestContext.Current.CancellationToken)).Hash);
  }

  [Theory]
  [InlineData("RS256")]
  [InlineData("PS256")]
  [InlineData("ES256")]
  public async Task Sign_ProducesVerifiableSignature(string algorithm)
  {
    var (signer, verifier) = Build(algorithm);

    var doc = Doc(new JObject { ["value"] = 42 });
    var signed = doc with { Signature = await signer.SignAsync(doc, null, TestContext.Current.CancellationToken) };

    signed.Signature!.Algorithm.ShouldBe(algorithm);
    verifier.Verify(signed).IsValid.ShouldBeTrue();
  }

  [Fact]
  public async Task Verify_DetectsTamperedPayload()
  {
    var (signer, verifier) = Build("RS256");

    var doc = Doc(new JObject { ["value"] = 42 });
    var signature = await signer.SignAsync(doc, null, TestContext.Current.CancellationToken);

    // keep the signature but change the data
    var tampered = doc with { Data = new JObject { ["value"] = 43 }, Signature = signature };

    verifier.Verify(tampered).IsValid.ShouldBeFalse();
  }

  [Fact]
  public async Task Sign_WithChaining_LinksToPrevious()
  {
    var (signer, _) = Build("RS256");

    var first = Doc(new JObject { ["n"] = 1 }, 0);
    var firstSig = await signer.SignAsync(first, null, TestContext.Current.CancellationToken);
    firstSig.PreviousSignature.ShouldBeNull();

    var second = Doc(new JObject { ["n"] = 2 }, 1);
    var secondSig = await signer.SignAsync(second, firstSig.Value, TestContext.Current.CancellationToken);
    secondSig.PreviousSignature.ShouldBe(firstSig.Value);
  }

  [Fact]
  public async Task AsyncCertificateFactory_IsUsed()
  {
    var cert = TestCertificate.CreateRsa();
    var services = new ServiceCollection();
    services.AddEventStoreSigning(o =>
      o.CertificateFactoryAsync = async (_, ct) =>
      {
        await Task.Yield();
        return cert;
      });
    var provider = services.BuildServiceProvider();
    var signer = provider.GetRequiredService<IEventSigner>();

    var doc = Doc(new JObject { ["value"] = 1 });
    var signature = await signer.SignAsync(doc, null, TestContext.Current.CancellationToken);

    signature.CertificateThumbprint.ShouldBe(cert.Thumbprint);
  }

  [Fact]
  public async Task AsyncCertificateResolver_IsUsedForVerification()
  {
    var cert = TestCertificate.CreateRsa();
    var resolverCalls = 0;
    var services = new ServiceCollection();
    services.AddEventStoreSigning(o =>
    {
      // no static Certificate: force both the async factory (sign) and async resolver (verify) paths
      o.CertificateFactoryAsync = (_, _) => ValueTask.FromResult(cert);
      o.CertificateResolverAsync = (thumbprint, _) =>
      {
        resolverCalls++;
        return ValueTask.FromResult<X509Certificate2?>(
          string.Equals(thumbprint, cert.Thumbprint, StringComparison.OrdinalIgnoreCase) ? cert : null);
      };
    });
    var provider = services.BuildServiceProvider();
    var signer = provider.GetRequiredService<IEventSigner>();
    var verifier = provider.GetRequiredService<IEventStreamSignatureVerifier>();

    var doc = Doc(new JObject { ["value"] = 7 });
    var signed = doc with { Signature = await signer.SignAsync(doc, null, TestContext.Current.CancellationToken) };

    var result = await verifier.VerifyAsync(new SingleDocumentStream(signed), TestContext.Current.CancellationToken);

    result.IsValid.ShouldBeTrue();
    resolverCalls.ShouldBeGreaterThan(0);
  }
}
