using AutoFixture.Xunit2;
using Microsoft.Extensions.Logging;
using Moq;
using Papst.EventStore.EventRegistration;
using Shouldly;
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
    resolved.ShouldNotBeNull();
    resolved.ShouldBe(writeName);
  }

  [Theory, AutoData]
  public void RegistrationShouldResolveReadName(string readName, string writeName)
  {
    var loggerMock = new Mock<ILogger<EventRegistrationTypeProvider>>();
    var registration = new EventDescriptionEventRegistration();

    registration.AddEvent<FooEvent>(new EventAttributeDescriptor(readName, false), new EventAttributeDescriptor(writeName, true));

    var eventreg = new EventRegistrationTypeProvider(loggerMock.Object, new[] { registration });

    var resolved = eventreg.ResolveIdentifier(readName);
    resolved.ShouldNotBeNull();
    resolved.ShouldBe(typeof(FooEvent));
  }

  private record FooEvent();
}
