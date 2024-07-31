// See https://aka.ms/new-console-template for more information


using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Papst.EventStore;
using Papst.EventStore.EntityFrameworkCore;
using Papst.EventStore.EventRegistration;

EventDescriptionEventRegistration registration = new();
registration.AddEvent<SampleEvent>(new EventAttributeDescriptor(nameof(SampleEvent), true));
registration.AddEvent<SampleEvent2>(new EventAttributeDescriptor(nameof(SampleEvent2), true));

ServiceProvider serviceProvider = new ServiceCollection()
  .AddEntityFrameworkCoreEventStore(options => options.UseSqlServer("Server=localhost;Database=EventStore;User Id=sa;Password=yourStrong1Pass;"))
  //.AddEntityFrameworkCoreEventStore(options => options.UseInMemoryDatabase("EventStore"))
  .AddEventRegistrationTypeProvider()
  .AddSingleton<IEventRegistration>(registration)
  .AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Trace))
  .BuildServiceProvider();

ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

IEventStore eventStore = serviceProvider.GetRequiredService<IEventStore>();

Guid streamId = Guid.NewGuid(); 
  //new("aa314ef1-1563-4568-b24a-a6b0192e4ab9");
IEventStream eventStream = await eventStore.CreateAsync(streamId, "TestEntity").ConfigureAwait(false);

await eventStream.AppendAsync(Guid.NewGuid(), new SampleEvent());
await eventStream.AppendAsync(Guid.NewGuid(), new SampleEvent2());


await foreach (var evt in eventStream.ListAsync(0))
{
  logger.LogInformation("Event {Event}", evt);
}

internal record SampleEvent();
internal record SampleEvent2();
