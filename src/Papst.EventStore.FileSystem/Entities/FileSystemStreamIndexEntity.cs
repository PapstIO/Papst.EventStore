using System;
using Papst.EventStore.Documents;

namespace Papst.EventStore.FileSystem.Entities;
internal record FileSystemStreamIndexEntity(
  Guid StreamId,
  DateTimeOffset Created,
  ulong Version,
  ulong NextVersion,
  DateTimeOffset Updated,
  string TargetType,
  ulong? LatestSnapshotVersion,
  EventStreamMetaData MetaData)
{
  /// <summary>
  /// Signature algorithm of the latest signed document, or <see langword="null"/>.
  /// </summary>
  public string? SigningAlgorithm { get; init; }

  /// <summary>
  /// Thumbprint of the certificate that signed the latest document.
  /// </summary>
  public string? SigningCertificateThumbprint { get; init; }

  /// <summary>
  /// The signature chain head: the <see cref="EventSignature.Value"/> of the
  /// latest signed document.
  /// </summary>
  public string? LatestSignature { get; init; }
}
