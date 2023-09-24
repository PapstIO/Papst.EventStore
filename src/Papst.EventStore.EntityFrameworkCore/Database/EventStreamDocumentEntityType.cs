namespace Papst.EventStore.EntityFrameworkCore.Database;

public enum EventStreamDocumentEntityType
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
