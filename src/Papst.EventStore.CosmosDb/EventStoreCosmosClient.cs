using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Papst.EventStore.CosmosDb
{
    /// <summary>
    /// Wrapper for the Cosmos Client to get a named instance
    /// </summary>
    internal class EventStoreCosmosClient : CosmosClient
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
        protected EventStoreCosmosClient()
        {
            Options = new CosmosEventStoreOptions();
        }
    }
}
