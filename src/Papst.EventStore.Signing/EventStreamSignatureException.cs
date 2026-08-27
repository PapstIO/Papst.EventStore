using System;
using Papst.EventStore.Exceptions;

namespace Papst.EventStore.Signing;

/// <summary>
/// Thrown when a document's signature fails verification during a verified read.
/// </summary>
public class EventStreamSignatureException : EventStreamException
{
  /// <summary>
  /// The version of the document whose signature failed to verify.
  /// </summary>
  public ulong Version { get; }

  /// <summary>
  /// Creates the exception for the given stream and version.
  /// </summary>
  public EventStreamSignatureException(Guid streamId, ulong version, string message)
    : base(streamId, message) => Version = version;
}
