using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Papst.EventStore.Signing;

/// <summary>
/// Maps a JWS-style algorithm identifier (e.g. <c>RS256</c>, <c>ES384</c>,
/// <c>PS256</c>) onto the concrete <see cref="X509Certificate2"/> signing and
/// verification primitives.
/// </summary>
internal static class SigningAlgorithm
{
  public static HashAlgorithmName GetHashAlgorithm(string algorithm)
  {
    string suffix = Suffix(algorithm);
    return suffix switch
    {
      "256" => HashAlgorithmName.SHA256,
      "384" => HashAlgorithmName.SHA384,
      "512" => HashAlgorithmName.SHA512,
      _ => throw new NotSupportedException($"Unsupported signature hash size in algorithm '{algorithm}'."),
    };
  }

  public static byte[] Sign(string algorithm, X509Certificate2 certificate, byte[] message)
  {
    HashAlgorithmName hash = GetHashAlgorithm(algorithm);
    string scheme = Scheme(algorithm);
    switch (scheme)
    {
      case "RS":
      case "PS":
        using (RSA? rsa = certificate.GetRSAPrivateKey())
        {
          if (rsa is null)
          {
            throw new InvalidOperationException("The signing certificate has no RSA private key.");
          }

          return rsa.SignData(message, hash, Padding(scheme));
        }
      case "ES":
        using (ECDsa? ecdsa = certificate.GetECDsaPrivateKey())
        {
          if (ecdsa is null)
          {
            throw new InvalidOperationException("The signing certificate has no ECDsa private key.");
          }

          return ecdsa.SignData(message, hash);
        }
      default:
        throw new NotSupportedException($"Unsupported signature scheme in algorithm '{algorithm}'.");
    }
  }

  public static bool Verify(string algorithm, X509Certificate2 certificate, byte[] message, byte[] signature)
  {
    HashAlgorithmName hash = GetHashAlgorithm(algorithm);
    string scheme = Scheme(algorithm);
    switch (scheme)
    {
      case "RS":
      case "PS":
        using (RSA? rsa = certificate.GetRSAPublicKey())
        {
          return rsa is not null && rsa.VerifyData(message, signature, hash, Padding(scheme));
        }
      case "ES":
        using (ECDsa? ecdsa = certificate.GetECDsaPublicKey())
        {
          return ecdsa is not null && ecdsa.VerifyData(message, signature, hash);
        }
      default:
        throw new NotSupportedException($"Unsupported signature scheme in algorithm '{algorithm}'.");
    }
  }

  private static RSASignaturePadding Padding(string scheme)
    => scheme == "PS" ? RSASignaturePadding.Pss : RSASignaturePadding.Pkcs1;

  private static string Scheme(string algorithm)
  {
    if (string.IsNullOrWhiteSpace(algorithm) || algorithm.Length < 2)
    {
      throw new NotSupportedException($"Invalid signature algorithm '{algorithm}'.");
    }

    return algorithm.Substring(0, 2).ToUpperInvariant();
  }

  private static string Suffix(string algorithm)
    => string.IsNullOrWhiteSpace(algorithm) || algorithm.Length < 3
      ? throw new NotSupportedException($"Invalid signature algorithm '{algorithm}'.")
      : algorithm.Substring(2);
}
