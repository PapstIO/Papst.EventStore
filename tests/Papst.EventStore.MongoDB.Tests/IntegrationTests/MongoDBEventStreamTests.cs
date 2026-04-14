using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.MongoDB.Tests.IntegrationTests.Events;
using Shouldly;
using Xunit;

namespace Papst.EventStore.MongoDB.Tests.IntegrationTests;

public class MongoDBEventStreamTests : IClassFixture<MongoDBIntegrationTestFixture>
{
  private readonly MongoDBIntegrationTestFixture _fixture;

  public MongoDBEventStreamTests(MongoDBIntegrationTestFixture fixture) => _fixture = fixture;

  [Theory, AutoData]
  public async Task ListAsync_ShouldReturnEmpty(Guid streamId)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    var stream = await eventStore.CreateAsync(streamId, "TestType", CancellationToken.None);

    // act
    var events = await stream.ListAsync(0, CancellationToken.None).ToListAsync(CancellationToken.None);

    // assert
    events.ShouldBeEmpty();
  }

  [Theory, AutoData]
  public async Task AppendAsync_ShouldAppendEvent(Guid streamId, TestEvent testEvent)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    var stream = await eventStore.CreateAsync(streamId, "TestType", CancellationToken.None);

    // act
    await stream.AppendAsync(Guid.NewGuid(), testEvent, cancellationToken: CancellationToken.None);

    // assert
    var events = await stream.ListAsync(0, CancellationToken.None).ToListAsync(CancellationToken.None);
    events.Count.ShouldBe(1);
    events[0].Data.Deserialize<TestEvent>().ShouldBe(testEvent);
  }

  [Theory, AutoData]
  public async Task ListAsync_ShouldReturnEntries(List<TestEvent> testEvents, Guid streamId)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    var stream = await eventStore.CreateAsync(streamId, "TestType", CancellationToken.None);

    foreach (var @event in testEvents)
    {
      await stream.AppendAsync(Guid.NewGuid(), @event, cancellationToken: CancellationToken.None);
    }

    // act
    var result = await stream.ListAsync(0, CancellationToken.None).ToListAsync(CancellationToken.None);

    // assert
    result.Count.ShouldBe(testEvents.Count);
    result.Select(e => e.Data.Deserialize<TestEvent>())
      .Where(e => e is not null)
      .Select(e => e!)
      .ToList()
      .ShouldBe(testEvents);
  }

  [Theory, AutoData]
  public async Task ListDescendingAsync_ShouldReturnEntriesInDescendingOrder(List<TestEvent> testEvents, Guid streamId)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    var stream = await eventStore.CreateAsync(streamId, "TestType", CancellationToken.None);

    foreach (var @event in testEvents)
    {
      await stream.AppendAsync(Guid.NewGuid(), @event, cancellationToken: CancellationToken.None);
    }

    // act
    var result = await stream.ListDescendingAsync((ulong)testEvents.Count, CancellationToken.None).ToListAsync(CancellationToken.None);

    // assert
    result.Count.ShouldBe(testEvents.Count);
    var versions = result.Select(e => e.Version).ToList();
    versions.ShouldBe(versions.OrderByDescending(version => version).ToList());
  }

  [Theory, AutoData]
  public async Task AppendSnapshotAsync_ShouldAppendSnapshot(Guid streamId, TestEvent snapshot)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    var stream = await eventStore.CreateAsync(streamId, "TestType", CancellationToken.None);

    // act
    await stream.AppendSnapshotAsync(Guid.NewGuid(), snapshot, cancellationToken: CancellationToken.None);

    // assert
    var latestSnapshot = await stream.GetLatestSnapshot(CancellationToken.None);
    latestSnapshot.ShouldNotBeNull();
    latestSnapshot!.Data.Deserialize<TestEvent>().ShouldBe(snapshot);
  }

  [Theory, AutoData]
  public async Task CreateTransactionalBatchAsync_ShouldCommitMultipleEvents(Guid streamId, List<TestEvent> testEvents)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    var stream = await eventStore.CreateAsync(streamId, "TestType", CancellationToken.None);

    // act
    var batch = await stream.CreateTransactionalBatchAsync();
    foreach (var @event in testEvents)
    {
      batch.Add(Guid.NewGuid(), @event);
    }
    await batch.CommitAsync(CancellationToken.None);

    // assert
    var result = await stream.ListAsync(0, CancellationToken.None).ToListAsync(CancellationToken.None);
    result.Count.ShouldBe(testEvents.Count);
  }
}
