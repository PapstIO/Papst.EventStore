using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.Aggregation.EventRegistration;
using Papst.EventStore.EntityFrameworkCore;
using Papst.EventStore.EntityFrameworkCore.Tests.IntegrationTests.Events;
using Papst.EventStore.EventRegistration;
using System;

namespace Papst.EventStore.EntityFrameworkCore.Tests;

public class EntityFrameworkCoreTestFixture
{
  public IServiceProvider BuildServiceProvider()
  {
    var services = new ServiceCollection();

    EventDescriptionEventRegistration registration = new();
    registration.AddEvent<TestEvent>(new EventAttributeDescriptor(nameof(TestEvent), true));

    services.AddEntityFrameworkCoreEventStore(options =>
      options.UseInMemoryDatabase(Guid.NewGuid().ToString())
    );

    services
      .AddEventRegistrationTypeProvider()
      .AddSingleton<IEventRegistration>(registration)
      .AddLogging();

    return services.BuildServiceProvider();
  }
}
