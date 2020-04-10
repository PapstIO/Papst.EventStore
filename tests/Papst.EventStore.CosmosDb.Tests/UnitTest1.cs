using Castle.Core.Logging;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Papst.EventStore.CosmosDb.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            //var dbRespMoq = new Mock<DatabaseResponse>();
            //var moq = new Mock<EventStoreCosmosClient>();
            //moq.Setup(x => x.CreateDatabaseIfNotExistsAsync("", null, null, default)).Returns(Task.FromResult(dbRespMoq.Object));
            //moq.Object.Options = new CosmosEventStoreOptions
            //{
            //    InitializeOnStartup = true
            //};

            //CosmosEventStore store = new CosmosEventStore(moq.Object, new Mock<ILogger<CosmosEventStore>>().Object);
            //var dbMoq = new Mock<EventStoreCosmosClient>();

            //dbMoq.Object.Should().NotBeNull();
        }
    }
}
