using System;
using System.Security.Cryptography;
using System.Text;
using Papst.EventStore.Documents;

namespace Papst.EventStore.Signing;

/// <summary>
/// Builds the exact byte sequence that is signed and verified for a document, so
/// the signer and the verifier always agree. The message binds the document's
/// identity (stream, version, type) and the hash of its canonical payload, and —
/// when chaining — the previous document's signature.
/// </summary>
internal static class SigningPayload
{
  /// <summary>
  /// Computes the canonical payload hash and the full message to sign/verify.
  /// </summary>
  /// <param name="document">The document being signed.</param>
  /// <param name="algorithm">The signature algorithm (selects the hash size).</param>
  /// <param name="previousSignature">The chain head, or <see langword="null"/>.</param>
  /// <returns>The base64 payload hash and the message bytes.</returns>
  public static (string Hash, byte[] Message) Build(
    EventStreamDocument document,
    string algorithm,
    string? previousSignature)
  {
    byte[] canonical = CanonicalJson.ToCanonicalUtf8(document.Data);
    HashAlgorithmName hashName = SigningAlgorithm.GetHashAlgorithm(algorithm);
    byte[] dataHash = ComputeHash(hashName, canonical);

    byte[] header = Encoding.UTF8.GetBytes(
      $"{document.StreamId:N}:{document.Version}:{document.DocumentType}:{document.DataType}:");
    byte[] previous = previousSignature is null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(previousSignature);

    byte[] message = new byte[header.Length + dataHash.Length + previous.Length];
    Buffer.BlockCopy(header, 0, message, 0, header.Length);
    Buffer.BlockCopy(dataHash, 0, message, header.Length, dataHash.Length);
    Buffer.BlockCopy(previous, 0, message, header.Length + dataHash.Length, previous.Length);

    return (Convert.ToBase64String(dataHash), message);
  }

  private static byte[] ComputeHash(HashAlgorithmName name, byte[] data)
  {
    using IncrementalHash hash = IncrementalHash.CreateHash(name);
    hash.AppendData(data);
    return hash.GetHashAndReset();
  }
}
