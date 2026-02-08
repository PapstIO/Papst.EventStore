using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.Aggregation.EventRegistration;
using Papst.EventStore.EventRegistration;
using Papst.EventStore.MongoDB.Tests.IntegrationTests.Events;
using Testcontainers.MongoDb;
using Xunit;

namespace Papst.EventStore.MongoDB.Tests;

public class MongoDBIntegrationTestFixture : IAsyncLifetime
{
  private readonly MongoDbContainer _mongoDbContainer = new MongoDbBuilder()
    .WithImage("mongo:8.0")
    .WithPortBinding(27017, true)
    .WithAutoRemove(true)
    .Build();

  public string ConnectionString => _mongoDbContainer.GetConnectionString();
  public const string DatabaseName = "EventStoreTest";

  public async Task InitializeAsync()
  {
    await _mongoDbContainer.StartAsync();
  }

  public IServiceProvider BuildServiceProvider()
  {
    var services = new ServiceCollection();

    EventDescriptionEventRegistration registration = new();
    registration.AddEvent<TestEvent>(new EventAttributeDescriptor(nameof(TestEvent), true));

    services.AddMongoDBEventStore(options =>
    {
      options.ConnectionString = ConnectionString;
      options.DatabaseName = DatabaseName;
    });

    services
      .AddEventRegistrationTypeProvider()
      .AddSingleton<IEventRegistration>(registration)
      .AddLogging();

    return services.BuildServiceProvider();
  }

  public async Task DisposeAsync()
  {
    await _mongoDbContainer.StopAsync();
    await _mongoDbContainer.DisposeAsync();
  }
}
