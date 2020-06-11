using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Papst.EventStore.Abstractions;
using Papst.EventStore.Abstractions.Extensions;
using Papst.EventStore.CosmosDb.Extensions;
using SampleCosmosEventStore.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SampleCosmosEventStore
{
    static class Program
    {
        private static readonly string _section = "cosmos";

        static async Task Main()
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
                // adds logging, needed for CosmosEventStore
                .AddLogging(opt =>
                {
                    opt.AddConsole();
                    // logs all output to the console
                    opt.SetMinimumLevel(LogLevel.Trace);
                })
                .BuildServiceProvider();

            IEventStore store = serviceProvider.GetRequiredService<IEventStore>();


            Guid streamGuid = Guid.NewGuid();
            var startEvent = new SampleCreatedEvent()
            {
                EventId = Guid.NewGuid(),
                Name = "Hallo",
                Foo = new Dictionary<string, object>()
                {
                    ["Foo"] = 124,
                    ["Bar"] = "baz"
                }
            };

            // creates the initial Stream
            IEventStream stream = await store.CreateEventStreamAsync<SampleCreatedEvent, SampleEntity>(
                streamGuid,
                nameof(SampleCreatedEvent),
                startEvent
            );

            var result = await store.AppendEventAsync<SampleAssociatedEvent, SampleEntity>(
                streamGuid,
                nameof(SampleAssociatedEvent),
                0,
                new SampleAssociatedEvent { Name = "Hallo" }
                );

            Console.WriteLine(JsonConvert.SerializeObject(result));

            result = await store.AppendEventAsync<SampleAssociatedEvent, SampleEntity>(
                streamGuid,
                nameof(SampleAssociatedEvent),
                1,
                new SampleAssociatedEvent { Name = "Papst" }
            );

            Console.WriteLine(JsonConvert.SerializeObject(result));

            result = await store.AppendEventAsync<SampleAssociationRemovedEvent, SampleEntity>(
                streamGuid,
                nameof(SampleAssociationRemovedEvent),
                2,
                new SampleAssociationRemovedEvent { Name = "Hallo" }
            );

            Console.WriteLine(JsonConvert.SerializeObject(result));

            stream = await store.ReadAsync(streamGuid);


            IEventStreamApplier<SampleEntity> applier = serviceProvider.GetRequiredService<IEventStreamApplier<SampleEntity>>();

            Console.WriteLine(JsonConvert.SerializeObject(applier.ApplyAsync(stream), Formatting.Indented));

        }
    }
}
