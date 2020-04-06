namespace Papst.EventStore.CosmosDb
{
    public class CosmosEventStoreOptions
    {
        public string Endpoint { get; set; }
        public string AccountSecret { get; set; }
        public bool InitializeOnStartup { get; set; }
        public string Collection { get; set; }
        public string Database { get; set; }
        public bool AllowTimeOverride { get; set; }
    }
}
