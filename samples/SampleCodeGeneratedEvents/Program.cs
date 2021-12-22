using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Papst.EventStore.Abstractions;
using Papst.EventStore.Abstractions.EventRegistration;
using Papst.EventStore.Abstractions.Extensions;
using Papst.EventStore.CosmosDb.Extensions;

namespace SampleCodeGeneratedEvents;

public static class Program
{
  private const string _section = "cosmos";

  public static async Task Main()
  {
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new[]
        {
          // Endpoint is local emulator
          new KeyValuePair<string, string>($"{_section}:Endpoint", "https://localhost:8081"),
          new KeyValuePair<string, string>($"{_section}:AccountSecret", "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="),
          new KeyValuePair<string, string>($"{_section}:InitializeOnStartup", "true"),
          new KeyValuePair<string, string>($"{_section}:Database", "EventStoreSample"),
          new KeyValuePair<string, string>($"{_section}:Collection", "Events"),
          new KeyValuePair<string, string>($"{_section}:StartVersion", "0") // start with version 1 for new streams
        }).Build();

    var serviceProvider = new ServiceCollection()
        // adds the cosmos event store
        .AddCosmosEventStore(config.GetSection(_section))
        // adds the Aggregator, that is using code generated events
        .AddEventStreamAggregator()
        // add code generated events from this assembly
        .AddCodeGeneratedEvents()
        // adds logging, needed for CosmosEventStore
        .AddLogging(opt =>
        {
          opt.AddConsole();
          // logs all output to the console
          opt.SetMinimumLevel(LogLevel.Trace);
        })
        .BuildServiceProvider();

    var registrations = serviceProvider.GetRequiredService<IEnumerable<IEventRegistration>>();


    await Task.CompletedTask;
  }
}

[EventName(Name = "Foo")]
public class MyEventSourcingEvent
{

}

public record FooEntity { }

public class MyTestEventAgg : EventAggregatorBase<FooEntity, MyEventSourcingEvent>
{
  public override Task<FooEntity?> ApplyAsync(MyEventSourcingEvent evt, FooEntity entity, IAggregatorStreamContext ctx)
  {
    throw new NotImplementedException();
  }
}

public class MyTestEventAgg2 : IEventAggregator<FooEntity, MyEventSourcingEvent>
{
  public Task<FooEntity?> ApplyAsync(MyEventSourcingEvent evt, FooEntity entity, IAggregatorStreamContext ctx)
  {
    throw new NotImplementedException();
  }

  public Task<FooEntity?> ApplyAsync(JObject evt, FooEntity entity, IAggregatorStreamContext ctx)
  {
    throw new NotImplementedException();
  }
}
