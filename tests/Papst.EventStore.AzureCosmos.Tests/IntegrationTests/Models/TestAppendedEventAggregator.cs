using Papst.EventStore.Aggregation;

namespace Papst.EventStore.AzureCosmos.Tests.IntegrationTests.Models;

public class TestAppendedEventAggregator : EventAggregatorBase<TestEntity, TestAppendedEvent>
{
  public override ValueTask<TestEntity?> ApplyAsync(TestAppendedEvent evt, TestEntity entity, IAggregatorStreamContext ctx)
  {
    entity.AppenedEvents.Add(nameof(TestAppendedEvent));
    return AsTask(entity);
  }
}
