using System;
using Papst.EventStore.Aggregation.EventRegistration;

namespace Papst.EventStore.EntityFrameworkCore.Tests.IntegrationTests.Events;

[EventName("TestEvent")]
public record TestEvent
{
  public string Test { get; init; } = Guid.NewGuid().ToString();
}
