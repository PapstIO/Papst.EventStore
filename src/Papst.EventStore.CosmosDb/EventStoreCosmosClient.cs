using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace Papst.EventStore.CosmosDb
{
    /// <summary>
    /// Wrapper for the Cosmos Client to get a named instance
    /// </summary>
    internal class EventStoreCosmosClient : CosmosClient
    {
        private readonly ILogger<EventStoreCosmosClient>? _logger;

        public CosmosEventStoreOptions Options { get; internal set; }

        public bool IsAlreadyInitialized { get; private set; } = false;

        public EventStoreCosmosClient(
            ILogger<EventStoreCosmosClient> logger,
            IOptions<CosmosEventStoreOptions> options
        )
            : base(options.Value.Endpoint, options.Value.AccountSecret)
        {
            _logger = logger;
            Options = options.Value;
        }

        /// <summary>
        /// Mockable Constructor
        /// </summary>
        protected EventStoreCosmosClient()
        {
            Options = new CosmosEventStoreOptions();
        }

        public async Task InitializeAsync(CancellationToken token)
        {
            if (!IsAlreadyInitialized)
            {
                _logger?.LogInformation("Initializing Database");

                DatabaseResponse db = await CreateDatabaseIfNotExistsAsync(Options.Database, cancellationToken: token).ConfigureAwait(false);
                if (db.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    _logger?.LogInformation("Created Database {Database} in Cosmos DB", Options.Database);
                }
                ContainerResponse container = await db.Database.CreateContainerIfNotExistsAsync(Options.Collection, "/StreamId", cancellationToken: token).ConfigureAwait(false);
                if (container.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    _logger?.LogInformation("Created Container {Container} in {Database}", Options.Collection, Options.Database);
                }
                IsAlreadyInitialized = true;
            }
        }
    }
}
