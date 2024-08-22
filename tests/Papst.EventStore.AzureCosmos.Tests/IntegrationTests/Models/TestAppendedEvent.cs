using Papst.EventStore.Aggregation.EventRegistration;

namespace Papst.EventStore.AzureCosmos.Tests.IntegrationTests.Models;

[EventName("Appended")]
public record TestAppendedEvent();
