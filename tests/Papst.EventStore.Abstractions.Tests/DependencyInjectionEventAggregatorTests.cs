using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using Papst.EventStore.Abstractions.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Papst.EventStore.Abstractions.Tests
{
    public class DependencyInjectionEventAggregatorTests
    {
        [Fact]
        public async Task ShouldApply()
        {
            var loggerMock = new Mock<ILogger<DependencyInjectionEventAggregator<TestEntity>>>();

            IServiceProvider services = ((IServiceCollection)new ServiceCollection())
               .AddSingleton<ILogger<DependencyInjectionEventAggregator<TestEntity>>>(loggerMock.Object)
               .Configure<EventStoreOptions>(options => options.StartVersion = 0)
               .AddEventStreamAggregator(GetType().Assembly)
               .BuildServiceProvider();

            var applier = services.GetRequiredService<IEventStreamAggregator<TestEntity>>();
            var applierInstance = services.GetRequiredService<IEventAggregator<TestEntity, TestSelfVersionIncrementingEvent>>();

            applierInstance.Should().NotBeNull();

            var mock = new Mock<IEventStream>();
            mock.Setup(x => x.Stream).Returns(() => new List<EventStreamDocument>() {new EventStreamDocument {
                Data = JObject.FromObject(new TestSelfVersionIncrementingEvent()),
                DataType = typeof(TestSelfVersionIncrementingEvent)
            } });

            TestEntity entity = new TestEntity { Foo = 15 };

            entity = await applier.AggregateAsync(mock.Object, entity).ConfigureAwait(false);

            entity.Foo.Should().Be(16);
        }

        [Theory, AutoData]
        public async Task ShouldDeleteEntity(TestEntity entity)
        {
            Mock<ILogger<DependencyInjectionEventAggregator<TestEntity>>> loggerMock = new Mock<ILogger<DependencyInjectionEventAggregator<TestEntity>>>();
            IServiceProvider services = ((IServiceCollection)new ServiceCollection())
                .AddSingleton<ILogger<DependencyInjectionEventAggregator<TestEntity>>>(loggerMock.Object)
                .Configure<EventStoreOptions>(options => options.StartVersion = 0)
                .AddEventStreamAggregator(GetType().Assembly)
                .BuildServiceProvider();

            var applier = services.GetRequiredService<IEventStreamAggregator<TestEntity>>();
            var applierInstance = services.GetRequiredService<IEventAggregator<TestEntity, TestSelfVersionIncrementingEvent>>();

            var mock = new Mock<IEventStream>();
            mock.Setup(x => x.Stream).Returns(() => new List<EventStreamDocument>()
            {
                new EventStreamDocument { Data = JObject.FromObject(new TestEvent()), DataType = typeof(TestEvent) },
                new EventStreamDocument { Data = JObject.FromObject(new TestDeletedEvent()), DataType = typeof(TestDeletedEvent) }
            });

            entity = await applier.AggregateAsync(mock.Object, entity).ConfigureAwait(false);

            entity.Should().BeNull();
        }

        [Theory, AutoData]
        public async Task ShouldResumeAfterDeletion(TestEntity entity)
        {
            Mock<ILogger<DependencyInjectionEventAggregator<TestEntity>>> loggerMock = new Mock<ILogger<DependencyInjectionEventAggregator<TestEntity>>>();
            IServiceProvider services = ((IServiceCollection)new ServiceCollection())
                .AddSingleton<ILogger<DependencyInjectionEventAggregator<TestEntity>>>(loggerMock.Object)
                .Configure<EventStoreOptions>(options => options.StartVersion = 0)
                .AddEventStreamAggregator(GetType().Assembly)
                .BuildServiceProvider();

            var applier = services.GetRequiredService<IEventStreamAggregator<TestEntity>>();
            var applierInstance = services.GetRequiredService<IEventAggregator<TestEntity, TestSelfVersionIncrementingEvent>>();

            var mock = new Mock<IEventStream>();
            mock.Setup(x => x.Stream).Returns(() => new List<EventStreamDocument>()
            {
                new EventStreamDocument { Data = JObject.FromObject(new TestEvent()), DataType = typeof(TestEvent) },
                new EventStreamDocument { Data = JObject.FromObject(new TestDeletedEvent()), DataType = typeof(TestDeletedEvent) },
                new EventStreamDocument { Data = JObject.FromObject(new TestRestoredEvent()), DataType = typeof(TestRestoredEvent)}
            });

            entity = await applier.AggregateAsync(mock.Object, entity).ConfigureAwait(false);

            entity.Should().NotBeNull();
        }

        [Theory, AutoData]
        public async Task ShouldIncrementVersion(ulong startVersion)
        {
            Mock<ILogger<DependencyInjectionEventAggregator<TestEntity>>> loggerMock = new Mock<ILogger<DependencyInjectionEventAggregator<TestEntity>>>();

            IServiceProvider services = ((IServiceCollection)new ServiceCollection())
                .AddSingleton<ILogger<DependencyInjectionEventAggregator<TestEntity>>>(loggerMock.Object)
                .Configure<EventStoreOptions>(options => options.StartVersion = 0)
                .AddEventStreamAggregator(GetType().Assembly)
                .BuildServiceProvider();
            var applier = services.GetRequiredService<IEventStreamAggregator<TestEntity>>();
            var applierInstance = services.GetRequiredService<IEventAggregator<TestEntity, TestSelfVersionIncrementingEvent>>();

            var mock = new Mock<IEventStream>();
            mock.Setup(x => x.Stream).Returns(() => new List<EventStreamDocument>() {new EventStreamDocument {
                Data = JObject.FromObject(new TestEvent()),
                DataType = typeof(TestEvent)
            } });

            TestEntity entity = new TestEntity { Foo = 15, Version = startVersion };

            entity = await applier.AggregateAsync(mock.Object, entity).ConfigureAwait(false);

            entity.Version.Should().Be(startVersion + 1);
        }

        [Fact]
        public async Task ShouldAggregateToVersion()
        {
            Mock<ILogger<DependencyInjectionEventAggregator<TestEntity>>> loggerMock = new Mock<ILogger<DependencyInjectionEventAggregator<TestEntity>>>();

            IServiceProvider services = ((IServiceCollection)new ServiceCollection())
                .AddSingleton<ILogger<DependencyInjectionEventAggregator<TestEntity>>>(loggerMock.Object)
                .Configure<EventStoreOptions>(options => options.StartVersion = 0)
                .AddEventStreamAggregator(GetType().Assembly)
                .BuildServiceProvider();
            var applier = services.GetRequiredService<IEventStreamAggregator<TestEntity>>();
            var applierInstance = services.GetRequiredService<IEventAggregator<TestEntity, TestSelfVersionIncrementingEvent>>();

            var mock = new Mock<IEventStream>();
            mock.Setup(x => x.Stream).Returns(() => new List<EventStreamDocument>()
            {
                new EventStreamDocument(){ Data = JObject.FromObject(new TestEvent()), DataType = typeof(TestEvent), Version = 0 },
                new EventStreamDocument(){ Data = JObject.FromObject(new TestEvent()), DataType = typeof(TestEvent), Version = 1 },
                new EventStreamDocument(){ Data = JObject.FromObject(new TestEvent()), DataType = typeof(TestEvent), Version = 2 },
                new EventStreamDocument(){ Data = JObject.FromObject(new TestEvent()), DataType = typeof(TestEvent), Version = 3 },
                new EventStreamDocument(){ Data = JObject.FromObject(new TestEvent()), DataType = typeof(TestEvent), Version = 4 },
                new EventStreamDocument(){ Data = JObject.FromObject(new TestEvent()), DataType = typeof(TestEvent), Version = 5 },
            });

            TestEntity entity = new TestEntity() { Foo = 15, Version = 0 };

            entity = await applier.AggregateAsync(mock.Object, entity, 4);
            entity.Version.Should().Be(4);
        }

        public class TestSelfVersionIncrementingEvent
        {
        }

        public class TestEntity : IEntity
        {
            public ulong Version { get; set; }
            public int Foo { get; set; }
        }

        private class TestSelfVersionIncrementingEventApplier : IEventAggregator<TestEntity, TestSelfVersionIncrementingEvent>
        {
            public Task<TestEntity> ApplyAsync(TestSelfVersionIncrementingEvent eventInstance, TestEntity entityInstance, IAggregatorStreamContext context)
            {
                entityInstance.Foo++;

                return Task.FromResult(entityInstance);
            }

            public Task<TestEntity> ApplyAsync(JObject eventInstance, TestEntity entityInstance, IAggregatorStreamContext context) => ApplyAsync(eventInstance.ToObject<TestSelfVersionIncrementingEvent>(), entityInstance, context);
        }

        private class TestEvent
        { }

        private class TestEventApplier : IEventAggregator<TestEntity, TestEvent>
        {
            public Task<TestEntity> ApplyAsync(TestEvent eventInstance, TestEntity entityInstance, IAggregatorStreamContext context)
            {
                return Task.FromResult(entityInstance);
            }

            public Task<TestEntity> ApplyAsync(JObject eventInstance, TestEntity entityInstance, IAggregatorStreamContext context)
                => ApplyAsync(eventInstance.ToObject<TestEvent>(), entityInstance, context);
        }

        private class TestDeletedEvent
        { }

        private class TestDeletedEventAggregator : IEventAggregator<TestEntity, TestDeletedEvent>
        {
            public Task<TestEntity> ApplyAsync(TestDeletedEvent evt, TestEntity entity, IAggregatorStreamContext ctx)
            {
                entity = null;

                return Task.FromResult(entity);
            }

            public Task<TestEntity> ApplyAsync(JObject evt, TestEntity entity, IAggregatorStreamContext ctx)
                => ApplyAsync(evt.ToObject<TestDeletedEvent>(), entity, ctx);
        }

        private class TestRestoredEvent { }

        private class TestRestoredEventAggregator : IEventAggregator<TestEntity, TestRestoredEvent>
        {
            public Task<TestEntity> ApplyAsync(TestRestoredEvent evt, TestEntity entity, IAggregatorStreamContext ctx)
            {
                entity = new TestEntity();

                return Task.FromResult(entity);
            }

            public Task<TestEntity> ApplyAsync(JObject evt, TestEntity entity, IAggregatorStreamContext ctx)
            => ApplyAsync(evt.ToObject<TestRestoredEvent>(), entity, ctx);
        }
    }
}
