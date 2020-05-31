using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Papst.EventStore.Abstractions.Extensions;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Papst.EventStore.Abstractions.Tests
{
    public class DependencyInjectionEventApplierTests
    {
        [Fact]
        public void TestApply()
        {
            var loggerMock = new Mock<ILogger<DependencyInjectionEventApplier<TestEntity>>>();

             IServiceProvider services = ((IServiceCollection)new ServiceCollection())
                .AddSingleton<ILogger<DependencyInjectionEventApplier<TestEntity>>>(loggerMock.Object)
                .AddEventStreamApplier(GetType().Assembly)
                .BuildServiceProvider();

            var applier = services.GetRequiredService<IEventStreamApplier<TestEntity>>();
            var applierInstance = services.GetRequiredService<IEventApplier<TestEntity, TestEvent>>();

            applierInstance.Should().NotBeNull();

            var mock = new Mock<IEventStream>();
            mock.Setup(x => x.Stream).Returns(() => new List<EventStreamDocument>() {new EventStreamDocument {
                Data = JObject.FromObject(new TestEvent()),
                DataType = typeof(TestEvent)
            } });

            TestEntity entity = new TestEntity { Foo = 15 };

            applier.Apply(mock.Object, entity);

            entity.Foo.Should().Be(16);
        }

        public class TestEvent
        {
        }

        public class TestEntity
        {
            public int Foo { get; set; }
        }

        private class TestEventApplier : IEventApplier<TestEntity, TestEvent>
        {
            public Task ApplyAsync(TestEvent eventInstance, TestEntity entityInstance)
            {
                entityInstance.Foo++;

                return Task.CompletedTask;
            }

            public Task ApplyAsync(JObject eventInstance, TestEntity entityInstance) => ApplyAsync(eventInstance.ToObject<TestEvent>(), entityInstance);
        }
    }
}
