using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.AzureCosmos.Tests.IntegrationTests.Models;
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
    stream.Version.Should().Be(1);
    var events = await stream.ListAsync().ToListAsync();
    events.Count.Should().Be(2);
    events.Should().Contain(d => d.Id == documentId);
  }
}
