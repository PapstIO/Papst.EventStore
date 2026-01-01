using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Papst.EventStore.EventRegistration;
using Xunit;

namespace Papst.EventStore.Tests;

public class EventRegistrationTests
{
  [Theory, AutoData]
  public void RegistrationShouldReturnWriteName(string readName, string writeName)
  {
    var loggerMock = new Mock<ILogger<EventRegistrationTypeProvider>>();
    var registration = new EventDescriptionEventRegistration();

    registration.AddEvent<FooEvent>(new EventAttributeDescriptor(readName, false), new EventAttributeDescriptor(writeName, true));

    var eventreg = new EventRegistrationTypeProvider(loggerMock.Object, new[] { registration });

    var resolved = eventreg.ResolveType(typeof(FooEvent));
    resolved.Should().NotBeNull();
    resolved.Should().Be(writeName);
  }

  [Theory, AutoData]
  public void RegistrationShouldResolveReadName(string readName, string writeName)
  {
    var loggerMock = new Mock<ILogger<EventRegistrationTypeProvider>>();
    var registration = new EventDescriptionEventRegistration();

    registration.AddEvent<FooEvent>(new EventAttributeDescriptor(readName, false), new EventAttributeDescriptor(writeName, true));

    var eventreg = new EventRegistrationTypeProvider(loggerMock.Object, new[] { registration });

    var resolved = eventreg.ResolveIdentifier(readName);
    resolved.Should().NotBeNull();
    resolved.Should().Be(typeof(FooEvent));
  }

  private record FooEvent();
}
