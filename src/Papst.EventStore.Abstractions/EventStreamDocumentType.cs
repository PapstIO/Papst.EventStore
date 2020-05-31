namespace Papst.EventStore.Abstractions
{
    /// <summary>
    /// Types of Documents
    /// </summary>
    public enum EventStreamDocumentType
    {
        /// <summary>
        /// A Document Snapshot containing the current Updated Document
        /// </summary>
        Snapshot,

        /// <summary>
        /// A Updating Event
        /// </summary>
        Event
    }
}