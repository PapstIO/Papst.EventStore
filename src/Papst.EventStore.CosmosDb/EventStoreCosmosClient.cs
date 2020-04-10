using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Papst.EventStore.CosmosDb
{
    public class EventStoreCosmosClient : CosmosClient
    {
        public CosmosEventStoreOptions Options { get; internal set; }

        public EventStoreCosmosClient(IOptions<CosmosEventStoreOptions> options)
            : base(options.Value.Endpoint, options.Value.AccountSecret)
        {
            Options = options.Value;
            
        }

        /// <summary>
        /// Mockable Constructor
        /// </summary>
        public EventStoreCosmosClient()
            : base()
        {
            Options = new CosmosEventStoreOptions();
        }
    }
}
