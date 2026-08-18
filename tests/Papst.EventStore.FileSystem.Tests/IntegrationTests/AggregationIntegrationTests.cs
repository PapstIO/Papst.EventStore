using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore;
using Papst.EventStore.Aggregation;
using Papst.EventStore.Aggregation.EventRegistration;
using Papst.EventStore.FileSystem;
using Papst.EventStore.Testing.Aggregation;
using Shouldly;
using Xunit;

namespace Papst.EventStore.FileSystem.Tests.IntegrationTests;

/// <summary>
/// Verifies the code-generated attribute based aggregation end to end against the FileSystem store.
/// </summary>
public class AggregationIntegrationTests : IDisposable
{
  private readonly string _tempPath = Path.Combine(Path.GetTempPath(), "EventStoreAggregationTests", Guid.NewGuid().ToString());

  public AggregationIntegrationTests() => Directory.CreateDirectory(_tempPath);

  [Fact]
  public async Task GeneratedAggregation_AggregatesStreamOntoEntity()
  {
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?> { ["Path"] = _tempPath })
      .Build();

    var services = new ServiceCollection();
    services.AddLogging();
    services.AddFileSystemEventStore(config);
    services.AddRegisteredEventAggregation();
    EventStoreEventAggregator.AddCodeGeneratedEvents(services);
    var provider = services.BuildServiceProvider();

    var store = provider.GetRequiredService<IEventStore>();
    var aggregator = provider.GetRequiredService<IEventStreamAggregator<SampleOrder>>();

    var streamId = Guid.NewGuid();
    var stream = await store.CreateAsync(streamId, "", CancellationToken.None);
    await stream.AppendAsync(Guid.NewGuid(), new SampleOrderCreated("Alice"), cancellationToken: CancellationToken.None);
    await stream.AppendAsync(Guid.NewGuid(), new SampleLineUpserted("SKU-1", 3), cancellationToken: CancellationToken.None);
    await stream.AppendAsync(Guid.NewGuid(), new SampleLineUpserted("SKU-1", 7), cancellationToken: CancellationToken.None);

    var order = await aggregator.AggregateAsync(stream, CancellationToken.None);

    order.ShouldNotBeNull();
    order!.Customer.ShouldBe("Alice");
    order.Lines.Count.ShouldBe(1);
    order.Lines[0].Quantity.ShouldBe(7);
  }

  public void Dispose()
  {
    if (Directory.Exists(_tempPath))
    {
      Directory.Delete(_tempPath, recursive: true);
    }
  }
}
