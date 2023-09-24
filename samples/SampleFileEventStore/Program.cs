using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Papst.EventStore;
using Papst.EventStore.EventRegistration;
using Papst.EventStore.FileSystem;

const string ConfigSection = "FileSystemEventStore";
string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

Directory.CreateDirectory(path);

try
{
  var config = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string>()
    {
      [$"{ConfigSection}:Path"] = path

    })
    .Build();

  EventDescriptionEventRegistration registration = new();
  registration.AddEvent<SampleEvent>(new EventAttributeDescriptor(nameof(SampleEvent), true));
  registration.AddEvent<SampleEvent2>(new EventAttributeDescriptor(nameof(SampleEvent2), true));

  ServiceProvider serviceProvider = new ServiceCollection()
    .AddFileSystemEventStore(config.GetSection(ConfigSection))
    .AddEventRegistrationTypeProvider()
    .AddSingleton<IEventRegistration>(registration)
    .AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Trace))
    .BuildServiceProvider();

  ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

  IEventStore eventStore = serviceProvider.GetRequiredService<IEventStore>();

  Guid streamId = new("aa314ef1-1563-4568-b24a-a6b0192e4ab9");
  IEventStream eventStream = await eventStore.CreateAsync(streamId, "TestEntity").ConfigureAwait(false);

  await eventStream.AppendAsync(Guid.NewGuid(), new SampleEvent());
  await eventStream.AppendAsync(Guid.NewGuid(), new SampleEvent2());


  await foreach (var evt in eventStream.ListAsync(0))
  {
    logger.LogInformation("Event {Event}", evt);
  }
}
finally
{
  Directory.Delete(path, true);
}


internal record SampleEvent();
internal record SampleEvent2();
