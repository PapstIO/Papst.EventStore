using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore;

namespace Papst.EventsStore.InMemory.Tests.IntegrationTests;

public class InMemoryEventStoreTests : IClassFixture<InMemoryTestFixture>
{
  private readonly InMemoryTestFixture _fixture;
  public InMemoryEventStoreTests(InMemoryTestFixture fixture) => _fixture = fixture;
  
  [Theory, AutoData]
  public async Task CreateAsync_ShouldCreateStream(Guid streamId)
  {
    // arrange
    var serviceProvider = _fixture.BuildServiceProvider();
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    
    // act
    await eventStore.CreateAsync(streamId, "", CancellationToken.None);
    
    // assert
    var stream = await eventStore.GetAsync(streamId, CancellationToken.None);
    stream.StreamId.Should().Be(streamId);
    stream.Version.Should().Be(0);
  }
  
  
}
