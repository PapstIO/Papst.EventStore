using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore;
using Papst.EventStore.Aggregation;
using Papst.EventStore.Aggregation.EventRegistration;
using Papst.EventStore.Documents;
using Papst.EventStore.EventRegistration;
using Papst.EventStore.InMemory;
using Shouldly;

namespace Papst.EventStore.Signing.Tests;

public class SigningPipelineInMemoryTests
{
  private static ServiceProvider BuildProvider(EventDescriptionEventRegistration? registration = null)
  {
    var cert = TestCertificate.CreateRsa();
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddInMemoryEventStore();
    services.AddEventRegistrationTypeProvider();
    services.AddRegisteredEventAggregation();
    registration ??= Registration();
    services.AddSingleton<IEventRegistration>(registration);
    services.AddEventStoreSigning(o => o.Certificate = cert);
    return services.BuildServiceProvider();
  }

  private static EventDescriptionEventRegistration Registration()
  {
    var registration = new EventDescriptionEventRegistration();
    registration.AddEvent<Incremented>(new EventAttributeDescriptor(nameof(Incremented), true));
    return registration;
  }

  [Fact]
  public async Task SingleAppend_SignsEveryDocument_AndChainVerifies()
  {
    var provider = BuildProvider();
    var store = provider.GetRequiredService<IEventStore>();
    var verifier = provider.GetRequiredService<IEventStreamSignatureVerifier>();

    var streamId = Guid.NewGuid();
    var stream = await store.CreateAsync(streamId, "counter", TestContext.Current.CancellationToken);
    for (int i = 0; i < 5; i++)
    {
      await stream.AppendAsync(Guid.NewGuid(), new Incremented(i), cancellationToken: TestContext.Current.CancellationToken);
    }

    var reopened = await store.GetAsync(streamId, TestContext.Current.CancellationToken);
    var docs = new List<EventStreamDocument>();
    await foreach (var doc in reopened.ListAsync(0, TestContext.Current.CancellationToken))
    {
      doc.Signature.ShouldNotBeNull();
      docs.Add(doc);
    }

    // chain linkage
    for (int i = 1; i < docs.Count; i++)
    {
      docs[i].Signature!.PreviousSignature.ShouldBe(docs[i - 1].Signature!.Value);
    }

    var result = await verifier.VerifyAsync(reopened, TestContext.Current.CancellationToken);
    result.IsValid.ShouldBeTrue();
    result.FirstInvalidVersion.ShouldBeNull();
  }

  [Fact]
  public async Task BatchAppend_SignsEveryDocument_AndVerifiesLikeSingle()
  {
    var provider = BuildProvider();
    var store = provider.GetRequiredService<IEventStore>();
    var verifier = provider.GetRequiredService<IEventStreamSignatureVerifier>();

    var streamId = Guid.NewGuid();
    var stream = await store.CreateAsync(streamId, "counter", TestContext.Current.CancellationToken);

    var ct = TestContext.Current.CancellationToken;
    var batch = await stream.CreateTransactionalBatchAsync();
    batch.Add(Guid.NewGuid(), new Incremented(1), cancellationToken: ct)
      .Add(Guid.NewGuid(), new Incremented(2), cancellationToken: ct)
      .Add(Guid.NewGuid(), new Incremented(3), cancellationToken: ct);
    await batch.CommitAsync(ct);

    var reopened = await store.GetAsync(streamId, TestContext.Current.CancellationToken);
    var result = await verifier.VerifyAsync(reopened, TestContext.Current.CancellationToken);
    result.IsValid.ShouldBeTrue();
  }

  [Fact]
  public async Task Aggregation_PopulatesSignedEntitySignature()
  {
    var registration = Registration();
    var cert = TestCertificate.CreateRsa();
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddInMemoryEventStore();
    services.AddEventRegistrationTypeProvider();
    services.AddRegisteredEventAggregation();
    services.AddSingleton<IEventRegistration>(registration);
    services.AddTransient<IEventAggregator<SignedCounter, Incremented>, IncrementAggregator>();
    services.AddEventStoreSigning(o => o.Certificate = cert);
    var provider = services.BuildServiceProvider();

    var store = provider.GetRequiredService<IEventStore>();
    var aggregator = provider.GetRequiredService<IEventStreamAggregator<SignedCounter>>();

    var streamId = Guid.NewGuid();
    var stream = await store.CreateAsync(streamId, "counter", TestContext.Current.CancellationToken);
    await stream.AppendAsync(Guid.NewGuid(), new Incremented(2), cancellationToken: TestContext.Current.CancellationToken);
    await stream.AppendAsync(Guid.NewGuid(), new Incremented(5), cancellationToken: TestContext.Current.CancellationToken);

    var reopened = await store.GetAsync(streamId, TestContext.Current.CancellationToken);
    var entity = await aggregator.AggregateAsync(reopened, TestContext.Current.CancellationToken);

    entity.ShouldNotBeNull();
    entity!.Count.ShouldBe(7);
    entity.Signature.ShouldNotBeNull();

    var lastDoc = await GetLastAsync(reopened, TestContext.Current.CancellationToken);
    entity.Signature!.Value.ShouldBe(lastDoc.Signature!.Value);
  }

  private static async Task<EventStreamDocument> GetLastAsync(IEventStream stream, CancellationToken ct)
  {
    EventStreamDocument? last = null;
    await foreach (var doc in stream.ListAsync(0, ct))
    {
      last = doc;
    }

    return last!;
  }

  [EventName(nameof(Incremented))]
  public record Incremented(int By);

  public record SignedCounter : ISignedEntity
  {
    public ulong Version { get; set; }
    public EventSignature? Signature { get; set; }
    public int Count { get; set; }
  }

  private sealed class IncrementAggregator : EventAggregatorBase<SignedCounter, Incremented>
  {
    public override ValueTask<SignedCounter?> ApplyAsync(Incremented evt, SignedCounter entity, IAggregatorStreamContext ctx)
    {
      entity.Count += evt.By;
      return AsTask(entity);
    }
  }
}
