using Azure.Core;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Papst.EventStore.CosmosDb.CosmosClients
{
    /// <summary>
    /// Cosmos Client Implementation based on Managed Identity Credentials
    /// </summary>
    internal class ManagedIdentityCosmosClient : EventStoreCosmosClientBase
    {
        public ManagedIdentityCosmosClient(
            ILogger<ManagedIdentityCosmosClient> logger,
            ManagedIdentityCosmosClientCredential credential,
            IOptions<CosmosEventStoreOptions> options
            )
            : base(
                logger,
                new CosmosClient(options.Value.Endpoint, credential.Credential),
                options.Value)
        { }
    }

    record ManagedIdentityCosmosClientCredential(TokenCredential Credential);
}
