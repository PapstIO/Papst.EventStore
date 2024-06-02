namespace Papst.EventStore.AzureCosmos;

public class CosmosEventStoreOptions
{
  public int ConcurrencyRetryCount { get; set; } = 3;
  public bool BuildIndexOnNotFound { get; set; }
}
