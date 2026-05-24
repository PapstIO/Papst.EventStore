using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.Aggregation.EventRegistration;
using Papst.EventStore.EventRegistration;
using Papst.EventStore.FileSystem;
using Papst.EventStore.FileSystem.Tests.IntegrationTests.Events;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Papst.EventStore.FileSystem.Tests;

public class FileSystemTestFixture : IDisposable
{
  private readonly string _tempPath = Path.Combine(Path.GetTempPath(), "EventStoreTests", Guid.NewGuid().ToString());

  public FileSystemTestFixture()
  {
    Directory.CreateDirectory(_tempPath);
  }

  public IServiceProvider BuildServiceProvider()
  {
    var services = new ServiceCollection();

    EventDescriptionEventRegistration registration = new();
    registration.AddEvent<TestEvent>(new EventAttributeDescriptor(nameof(TestEvent), true));

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?> { ["Path"] = _tempPath })
      .Build();

    services.AddFileSystemEventStore(config);

    services
      .AddEventRegistrationTypeProvider()
      .AddSingleton<IEventRegistration>(registration)
      .AddLogging();

    return services.BuildServiceProvider();
  }

  public void Dispose()
  {
    if (Directory.Exists(_tempPath))
    {
      Directory.Delete(_tempPath, recursive: true);
    }
  }
}
