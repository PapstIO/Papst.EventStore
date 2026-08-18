using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore;
using Papst.EventStore.Aggregation;
using Papst.EventStore.Aggregation.EventRegistration;
using Papst.EventStore.MongoDB;
using Papst.EventStore.Testing.Aggregation;
using Shouldly;
using Xunit;

namespace Papst.EventStore.MongoDB.Tests.IntegrationTests;

/// <summary>
/// Verifies the code-generated attribute based aggregation end to end against the MongoDB store
/// (requires a running Docker/Podman engine for Testcontainers).
/// </summary>
public class AggregationIntegrationTests : IClassFixture<MongoDBIntegrationTestFixture>
{
  private readonly MongoDBIntegrationTestFixture _fixture;

  public AggregationIntegrationTests(MongoDBIntegrationTestFixture fixture) => _fixture = fixture;

  [Fact]
  public async Task GeneratedAggregation_AggregatesStreamOntoEntity()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddMongoDBEventStore(options =>
    {
      options.ConnectionString = _fixture.ConnectionString;
      options.DatabaseName = MongoDBIntegrationTestFixture.DatabaseName;
    });
    services.AddRegisteredEventAggregation();
    EventStoreEventAggregator.AddCodeGeneratedEvents(services);
    var provider = services.BuildServiceProvider();

    var store = provider.GetRequiredService<IEventStore>();
    var aggregator = provider.GetRequiredService<IEventStreamAggregator<SampleOrder>>();

    var streamId = Guid.NewGuid();
    var stream = await store.CreateAsync(streamId, "TestType", CancellationToken.None);
    await stream.AppendAsync(Guid.NewGuid(), new SampleOrderCreated("Alice"), cancellationToken: CancellationToken.None);
    await stream.AppendAsync(Guid.NewGuid(), new SampleLineUpserted("SKU-1", 3), cancellationToken: CancellationToken.None);
    await stream.AppendAsync(Guid.NewGuid(), new SampleLineUpserted("SKU-1", 7), cancellationToken: CancellationToken.None);

    var order = await aggregator.AggregateAsync(stream, CancellationToken.None);

    order.ShouldNotBeNull();
    order!.Customer.ShouldBe("Alice");
    order.Lines.Count.ShouldBe(1);
    order.Lines[0].Quantity.ShouldBe(7);
  }
}
