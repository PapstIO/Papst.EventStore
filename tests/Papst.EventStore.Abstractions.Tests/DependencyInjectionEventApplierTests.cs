﻿using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Papst.EventStore.Abstractions.Extensions;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using AutoFixture.Xunit2;

namespace Papst.EventStore.Abstractions.Tests
{
    public class DependencyInjectionEventApplierTests
    {
        [Fact]
        public async Task ShouldApply()
        {
            var loggerMock = new Mock<ILogger<DependencyInjectionEventApplier<TestEntity>>>();

            IServiceProvider services = ((IServiceCollection)new ServiceCollection())
               .AddSingleton<ILogger<DependencyInjectionEventApplier<TestEntity>>>(loggerMock.Object)
               .AddEventStreamApplier(GetType().Assembly)
               .BuildServiceProvider();

            var applier = services.GetRequiredService<IEventStreamApplier<TestEntity>>();
            var applierInstance = services.GetRequiredService<IEventApplier<TestEntity, TestSelfVersionIncrementingEvent>>();

            applierInstance.Should().NotBeNull();

            var mock = new Mock<IEventStream>();
            mock.Setup(x => x.Stream).Returns(() => new List<EventStreamDocument>() {new EventStreamDocument {
                Data = JObject.FromObject(new TestSelfVersionIncrementingEvent()),
                DataType = typeof(TestSelfVersionIncrementingEvent)
            } });

            TestEntity entity = new TestEntity { Foo = 15 };

            entity = await applier.ApplyAsync(mock.Object, entity).ConfigureAwait(false);

            entity.Foo.Should().Be(16);
        }

        [Theory, AutoData]
        public async Task ShouldIncrementVersion(ulong startVersion)
        {
            var loggerMock = new Mock<ILogger<DependencyInjectionEventApplier<TestEntity>>>();

            IServiceProvider services = ((IServiceCollection)new ServiceCollection())
                .AddSingleton<ILogger<DependencyInjectionEventApplier<TestEntity>>>(loggerMock.Object)
                .AddEventStreamApplier(GetType().Assembly)
                .BuildServiceProvider();
            var applier = services.GetRequiredService<IEventStreamApplier<TestEntity>>();
            var applierInstance = services.GetRequiredService<IEventApplier<TestEntity, TestSelfVersionIncrementingEvent>>();

            var mock = new Mock<IEventStream>();
            mock.Setup(x => x.Stream).Returns(() => new List<EventStreamDocument>() {new EventStreamDocument {
                Data = JObject.FromObject(new TestEvent()),
                DataType = typeof(TestEvent)
            } });

            TestEntity entity = new TestEntity { Foo = 15, Version = startVersion };

            entity = await applier.ApplyAsync(mock.Object, entity).ConfigureAwait(false);

            entity.Version.Should().Be(startVersion + 1);
        }

        public class TestSelfVersionIncrementingEvent
        {
        }

        public class TestEntity : IEntity
        {
            public ulong Version { get; set; }
            public int Foo { get; set; }
        }

        private class TestSelfVersionIncrementingEventApplier : IEventApplier<TestEntity, TestSelfVersionIncrementingEvent>
        {
            public Task<TestEntity> ApplyAsync(TestSelfVersionIncrementingEvent eventInstance, TestEntity entityInstance)
            {
                entityInstance.Foo++;

                return Task.FromResult(entityInstance);
            }

            public Task<TestEntity> ApplyAsync(JObject eventInstance, TestEntity entityInstance) => ApplyAsync(eventInstance.ToObject<TestSelfVersionIncrementingEvent>(), entityInstance);
        }

        private class TestEvent
        { }

        private class TestEventApplier : IEventApplier<TestEntity, TestEvent>
        {
            public Task<TestEntity> ApplyAsync(TestEvent eventInstance, TestEntity entityInstance)
            {
                return Task.FromResult(entityInstance);
            }

            public Task<TestEntity> ApplyAsync(JObject eventInstance, TestEntity entityInstance)
                => ApplyAsync(eventInstance.ToObject<TestEvent>(), entityInstance);
        }
    }
}
