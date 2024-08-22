namespace Papst.EventStore.Documents;

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
  Event,
  
  /// <summary>
  /// The Index Document of the stream
  /// </summary>
  Index
}
