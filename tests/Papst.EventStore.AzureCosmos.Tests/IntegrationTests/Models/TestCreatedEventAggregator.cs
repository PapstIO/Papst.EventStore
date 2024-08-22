using Papst.EventStore.Aggregation;

namespace Papst.EventStore.AzureCosmos.Tests.IntegrationTests.Models;

public class TestCreatedEventAggregator : EventAggregatorBase<TestEntity, TestCreatedEvent>
{
  public override ValueTask<TestEntity?> ApplyAsync(TestCreatedEvent evt, TestEntity entity, IAggregatorStreamContext ctx)
  {
    return AsTask(entity);
  }
}
