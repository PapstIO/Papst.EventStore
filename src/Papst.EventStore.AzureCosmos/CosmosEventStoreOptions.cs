namespace Papst.EventStore.AzureCosmos;

public class CosmosEventStoreOptions
{
  /// <summary>
  /// Count of retries when two parties are updating the same stream to avoid concurrency issues
  /// </summary>
  public int ConcurrencyRetryCount { get; set; } = 3;
  
  /// <summary>
  /// Whether to try to build the index for an existing stream, when no index is found
  /// or to return a <see cref="Papst.EventStore.Exceptions.EventStreamNotFoundException"/>
  /// </summary>
  public bool BuildIndexOnNotFound { get; set; }
  
  /// <summary>
  /// Flag if the Tenant Id of the Stream Meta Data should be updated when appending a new event
  /// </summary>
  public bool UpdateTenantIdOnAppend { get; set; } = true;
}
