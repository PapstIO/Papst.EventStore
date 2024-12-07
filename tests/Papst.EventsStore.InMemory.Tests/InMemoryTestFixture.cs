using Microsoft.Extensions.DependencyInjection;
using Papst.EventsStore.InMemory.Tests.IntegrationTests.Events;
using Papst.EventStore;
using Papst.EventStore.EventRegistration;
using Papst.EventStore.InMemory;

namespace Papst.EventsStore.InMemory.Tests;

public class InMemoryTestFixture
{
  public IServiceCollection Services { get; }

  private readonly Lazy<IServiceProvider> _serviceProvider;

  public InMemoryTestFixture()
  {
    Services = new ServiceCollection();
    _serviceProvider = new(() => Services.BuildServiceProvider());
  }
  
  public IServiceProvider BuildServiceProvider()
  {
    if (_serviceProvider.IsValueCreated)
    {
      return _serviceProvider.Value;
    }

    EventDescriptionEventRegistration registration = new();
    registration.AddEvent<TestEvent>(new EventAttributeDescriptor(nameof(TestEvent), true));
    
    Services.AddInMemoryEventStore()
      .AddEventRegistrationTypeProvider()
      .AddSingleton<IEventRegistration>(registration)
      .AddLogging()
      ;
    return _serviceProvider.Value;
  }
}
