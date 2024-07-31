using Papst.EventStore.Documents;

namespace Papst.EventStore.AzureCosmos;


internal class StreamIdEventTypeIdStrategy : ICosmosIdStrategy
{
  public ValueTask<string> GenerateIdAsync(Guid streamId, ulong version, EventStreamDocumentType type) 
    => ValueTask.FromResult($"{streamId}|{(type == EventStreamDocumentType.Event ? "Document" : "Snapshot")}|{version}");
}
