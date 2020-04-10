using AutoFixture.Xunit2;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Papst.EventStore.Abstractions.Tests
{
    public class IEventStoreExtensionsTests
    {
        [Theory, AutoData]
        public async Task TestAppendEventAsync(string name, object testDocument, Guid id, ulong expected)
        {
            Mock<IEventStore> storeMock = new Mock<IEventStore>();
            storeMock.Setup(x => x.AppendAsync(Guid.NewGuid(), 0, new EventStreamDocument(), default)).Callback(() => Task.FromResult<EventStoreResult>(new EventStoreResult { }));
            

            
        }
    }
}
