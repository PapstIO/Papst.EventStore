using AutoFixture.Xunit2;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Papst.EventStore.Aggregation;
using Papst.EventStore.AzureCosmos.Tests.IntegrationTests.Models;
using Papst.EventStore.Documents;
using Shouldly;
using System.Linq;
using Xunit;

namespace Papst.EventStore.AzureCosmos.Tests.IntegrationTests;

public class CosmosEventStreamTests : IClassFixture<CosmosDbIntegrationTestFixture>
{
  private readonly CosmosDbIntegrationTestFixture _fixture;

  public CosmosEventStreamTests(CosmosDbIntegrationTestFixture fixture) => _fixture = fixture;

  private async Task<IEventStream> CreateStreamAsync(IEventStore store, Guid streamId, params TestAppendedEvent[] events)
  {
    var stream = await store.CreateAsync(streamId, nameof(TestEntity));

    await stream.AppendAsync(Guid.NewGuid(), new TestCreatedEvent());

    foreach (TestAppendedEvent evt in events)
    {
      await stream.AppendAsync(Guid.NewGuid(), evt);
    }

    return stream;
  }

  private static void ShouldMatch(EventStreamMetaData actual, EventStreamMetaData expected)
  {
    actual.UserId.ShouldBe(expected.UserId);
    actual.UserName.ShouldBe(expected.UserName);
    actual.TenantId.ShouldBe(expected.TenantId);
    actual.Comment.ShouldBe(expected.Comment);
    if (expected.Additional is null)
    {
      actual.Additional.ShouldBeNull();
      return;
    }

    actual.Additional.ShouldNotBeNull();
    actual.Additional.Count.ShouldBe(expected.Additional.Count);
    foreach (var entry in expected.Additional)
    {
      actual.Additional[entry.Key].ShouldBe(entry.Value);
    }
  }

  [Theory, AutoData]
  public async Task AppendAsync_ShouldAppendEvent(Guid streamId, Guid documentId)
  {
    // arrange
    var services = _fixture.BuildServiceProvider();
    var store = services.GetRequiredService<IEventStore>();
    var stream = await CreateStreamAsync(store, streamId);

    // act
    await stream.AppendAsync(documentId, new TestAppendedEvent());

    // assert
    stream.Version.ShouldBe(1UL);
    var events = await stream.ListAsync().ToListAsync();
    events.ShouldContain(d => d.Id == documentId);
  }

  [Theory, AutoData]
  public async Task LowLevelAppendAsync_ShouldAppendEvent(Guid streamId, Guid documentId)
  {
    var services = _fixture.BuildServiceProvider();
    var store = services.GetRequiredService<IEventStore>();
    var stream = await CreateStreamAsync(store, streamId);
    var lowLevelStream = stream.ShouldBeAssignableTo<ILowLevelEventStream>();
    JObject payload = new()
    {
      ["value"] = "low-level"
    };

    await lowLevelStream.AppendAsync(documentId, "LowLevelEvent", payload);

    stream.Version.ShouldBe(1UL);
    var events = await stream.ListAsync().ToListAsync();
    events.ShouldContain(d => d.Id == documentId);
    EventStreamDocument appendedEvent = events.Single(d => d.Id == documentId);
    appendedEvent.DataType.ShouldBe("LowLevelEvent");
    appendedEvent.Name.ShouldBe("LowLevelEvent");
    appendedEvent.Data.ShouldBe(payload);
  }

  [Theory, AutoData]
  public async Task AppendTransactionAsync_ShouldAppendEvents(Guid streamId)
  {
    // arrange
    var services = _fixture.BuildServiceProvider();
    var store = services.GetRequiredService<IEventStore>();
    var stream = await CreateStreamAsync(store, streamId);

    // act
    var batch = await stream.CreateTransactionalBatchAsync();
    batch.Add(Guid.NewGuid(), new TestAppendedEvent());
    batch.Add(Guid.NewGuid(), new TestAppendedEvent());
    batch.Add(Guid.NewGuid(), new TestAppendedEvent());
    batch.Add(Guid.NewGuid(), new TestAppendedEvent());
    await batch.CommitAsync();

    // assert
    var events = await stream.ListAsync(0).ToListAsync();
    events.Count.ShouldBe(5);

  }

  [Theory, AutoData]
  public async Task AppenSnapshotAsync_ShouldAppendSnapshot(Guid streamId, Guid documentId)
  {
    // arrange
    var services = _fixture.BuildServiceProvider();
    var store = services.GetRequiredService<IEventStore>();
    var stream = await CreateStreamAsync(store, streamId);
    var aggregator = services.GetRequiredService<IEventStreamAggregator<TestEntity>>();
    var entity = await aggregator.AggregateAsync(stream, CancellationToken.None);

    // act
    Func<Task> act = async () =>
    {
      await stream.AppendSnapshotAsync(documentId, entity!, null, CancellationToken.None);
      entity = await aggregator.AggregateAsync(stream, CancellationToken.None);
    };

    // assert
    await Should.NotThrowAsync(act);
    entity.ShouldNotBeNull();
    stream.LatestSnapshotVersion.HasValue.ShouldBeTrue();
    stream.LatestSnapshotVersion.ShouldBe(entity.Version);
  }

  [Theory, AutoData]
  public async Task ListAsync_ShouldListEvents(Guid streamId)
  {
    // arrange
    var services = _fixture.BuildServiceProvider();
    var store = services.GetRequiredService<IEventStore>();
    var stream = await CreateStreamAsync(store, streamId, new TestAppendedEvent(), new TestAppendedEvent(), new TestAppendedEvent());

    // act
    var list = stream.ListAsync(0);
    var events = await list.ToListAsync();

    // assert
    events.Count.ShouldBe(4);
  }

  [Theory, AutoData]
  public async Task UpdateMetadata_ShouldUpdate(Guid streamId, EventStreamMetaData metaData)
  {
    // arrange
    var services = _fixture.BuildServiceProvider();
    var store = services.GetRequiredService<IEventStore>();
    var stream = await CreateStreamAsync(store, streamId, new TestAppendedEvent());

    // act
    await stream.UpdateStreamMetaData(metaData, default);

    // assert
    stream.MetaData.ShouldNotBeNull();
    ShouldMatch(stream.MetaData, metaData);
  }

  [Theory, AutoData]
  public async Task UpdateMetadata_ShouldPersist(Guid streamId, EventStreamMetaData metaData)
  {
    // arrange
    var services = _fixture.BuildServiceProvider();
    var store = services.GetRequiredService<IEventStore>();
    var stream = await CreateStreamAsync(store, streamId, new TestAppendedEvent());

    // act
    await stream.UpdateStreamMetaData(metaData, default);
    // enforce reload
    stream = await store.GetAsync(streamId, default);

    // assert
    stream.MetaData.ShouldNotBeNull();
    ShouldMatch(stream.MetaData, metaData);
  }
}
