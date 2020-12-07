namespace Papst.EventStore.CosmosDb
{
    /// <summary>
    /// Configuration for the Cosmos Database Connection
    /// </summary>
    public class CosmosEventStoreOptions
    {
        /// <summary>
        /// Endpoint URL
        /// </summary>
        public string Endpoint { get; set; } = null!;

        /// <summary>
        /// Secret Identifier
        /// </summary>
        public string AccountSecret { get; set; } = null!;

        /// <summary>
        /// Whether to try to create Database and collection after creation of the Client
        /// </summary>
        public bool InitializeOnStartup { get; set; }

        /// <summary>
        /// Name of the Collection
        /// </summary>
        public string Collection { get; set; } = null!;

        /// <summary>
        /// Name of the Database
        /// </summary>
        public string Database { get; set; } = null!;

        /// <summary>
        /// Whether to allow a Event Timestamp to be set from the outside or 
        /// to override the time before inserting the document
        /// </summary>
        public bool AllowTimeOverride { get; set; }
    }
}
