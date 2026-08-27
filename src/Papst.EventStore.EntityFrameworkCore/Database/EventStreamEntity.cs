namespace Papst.EventStore.EntityFrameworkCore.Database;

public class EventStreamEntity
{
  public Guid StreamId { get; init; }
  public DateTimeOffset Created { get; init; }
  public ulong Version { get; set; }
  public ulong NextVersion { get; set; }
  public DateTimeOffset Updated { get; set; }
  public string TargetType { get; init; } = string.Empty;
  public ulong?  LatestSnapshotVersion { get; set; }
  
  /// <summary>
  /// UserId for Meta Data of Stream
  /// </summary>
  public string? MetaDataUserId { get; set; }
  
  /// <summary>
  /// Username for Meta Data of Stream
  /// </summary>
  public string? MetaDataUserName { get; set; }
  
  /// <summary>
  /// Tenant Id for Meta Data of Stream
  /// </summary>
  public string? MetaDataTenantId { get; set; }
  
  /// <summary>
  /// Stream Comment for Meta Data of Stream
  /// </summary>
  public string? MetaDataComment { get; set; }
  
  /// <summary>
  /// Additional Meta Data encoded as JSON Dictionary for the Stream
  /// </summary>
  public string? MetaDataAdditionJson { get; set; }

  /// <summary>
  /// Signature algorithm of the latest signed document.
  /// </summary>
  public string? SigningAlgorithm { get; set; }

  /// <summary>
  /// Thumbprint of the certificate that signed the latest document.
  /// </summary>
  public string? SigningCertificateThumbprint { get; set; }

  /// <summary>
  /// The signature chain head: the signature value of the latest signed document.
  /// </summary>
  public string? LatestSignature { get; set; }
}
