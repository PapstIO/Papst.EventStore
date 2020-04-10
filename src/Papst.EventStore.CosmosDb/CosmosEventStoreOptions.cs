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
        public string Endpoint { get; set; }

        /// <summary>
        /// Secret Identifier
        /// </summary>
        public string AccountSecret { get; set; }

        /// <summary>
        /// Whether to try to create Database and collection after creation of the Client
        /// </summary>
        public bool InitializeOnStartup { get; set; }

        /// <summary>
        /// Name of the Collection
        /// </summary>
        public string Collection { get; set; }

        /// <summary>
        /// Name of the Database
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Whether to allow a Event Timestamp to be set from the outside or 
        /// to override the time before inserting the document
        /// </summary>
        public bool AllowTimeOverride { get; set; }
    }
}
