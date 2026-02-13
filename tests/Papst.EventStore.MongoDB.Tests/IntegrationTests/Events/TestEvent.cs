using Papst.EventStore.Aggregation.EventRegistration;

namespace Papst.EventStore.MongoDB.Tests.IntegrationTests.Events;

[EventName("TestEvent")]
public record TestEvent
{
  public string Test { get; init; } = System.Guid.NewGuid().ToString();
}
