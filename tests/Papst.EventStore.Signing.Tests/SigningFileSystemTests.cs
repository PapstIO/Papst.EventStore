using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore;
using Papst.EventStore.Aggregation.EventRegistration;
using Papst.EventStore.Documents;
using Papst.EventStore.EventRegistration;
using Papst.EventStore.FileSystem;
using Shouldly;

namespace Papst.EventStore.Signing.Tests;

/// <summary>
/// Verifies that a signature survives the FileSystem store's System.Text.Json
/// serialization round-trip and still verifies after being read back (risk #7).
/// </summary>
public class SigningFileSystemTests : IDisposable
{
  private readonly string _path = Path.Combine(Path.GetTempPath(), "papst-signing-" + Guid.NewGuid().ToString("N"));

  [Fact]
  public async Task SignedEvents_SurviveFileSystemRoundTrip_AndVerify()
  {
    var ct = TestContext.Current.CancellationToken;
    var cert = TestCertificate.CreateRsa();

    var registration = new EventDescriptionEventRegistration();
    registration.AddEvent<FsEvent>(new EventAttributeDescriptor(nameof(FsEvent), true));

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?> { ["Path"] = _path })
      .Build();

    var services = new ServiceCollection();
    services.AddLogging();
    services.AddFileSystemEventStore(config);
    services.AddEventRegistrationTypeProvider();
    services.AddSingleton<IEventRegistration>(registration);
    services.AddEventStoreSigning(o => o.Certificate = cert);
    var provider = services.BuildServiceProvider();

    var store = provider.GetRequiredService<IEventStore>();
    var verifier = provider.GetRequiredService<IEventStreamSignatureVerifier>();

    var streamId = Guid.NewGuid();
    var stream = await store.CreateAsync(streamId, "fs", ct);
    await stream.AppendAsync(Guid.NewGuid(), new FsEvent("a"), cancellationToken: ct);
    await stream.AppendAsync(Guid.NewGuid(), new FsEvent("b"), cancellationToken: ct);

    // Re-open from disk so the documents are actually deserialized.
    var reopened = await store.GetAsync(streamId, ct);

    var docs = new List<EventStreamDocument>();
    await foreach (var doc in reopened.ListAsync(0, ct))
    {
      doc.Signature.ShouldNotBeNull();
      docs.Add(doc);
    }

    docs.Count.ShouldBe(2);
    docs[1].Signature!.PreviousSignature.ShouldBe(docs[0].Signature!.Value);

    var result = await verifier.VerifyAsync(reopened, ct);
    result.IsValid.ShouldBeTrue();
  }

  public void Dispose()
  {
    if (Directory.Exists(_path))
    {
      Directory.Delete(_path, recursive: true);
    }
  }

  [EventName(nameof(FsEvent))]
  public record FsEvent(string Value);
}
