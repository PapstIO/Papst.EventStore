using System.Threading;
using System.Threading.Tasks;
using Papst.EventStore.Documents;
using Papst.EventStore.Pipeline;

namespace Papst.EventStore.Signing;

/// <summary>
/// The append pipeline handler that signs every appended document. For a batch it
/// signs each entry in turn, chaining onto the running head, so batched and
/// single-appended events verify through one identical code path.
/// </summary>
internal sealed class SigningPipelineHandler : IEventStorePipelineHandler<EventAppendContext>
{
  private readonly IEventSigner _signer;

  public SigningPipelineHandler(IEventSigner signer) => _signer = signer;

  public async Task HandleAsync(EventAppendContext context, EventStorePipelineDelegate next, CancellationToken cancellationToken)
  {
    string? previous = context.PreviousSignature;
    foreach (EventAppendEntry entry in context.Entries)
    {
      EventSignature signature = await _signer.SignAsync(entry.Document, previous, cancellationToken).ConfigureAwait(false);
      entry.Document = entry.Document with { Signature = signature };
      previous = signature.Value;
    }

    await next().ConfigureAwait(false);
  }
}
