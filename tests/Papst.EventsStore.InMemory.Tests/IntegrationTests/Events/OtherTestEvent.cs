using Papst.EventStore.Aggregation.EventRegistration;

namespace Papst.EventsStore.InMemory.Tests.IntegrationTests.Events;

[EventName("OtherTestEvent")]
public record OtherTestEvent
{
  public string Test { get; init; } = Guid.NewGuid().ToString();
}
