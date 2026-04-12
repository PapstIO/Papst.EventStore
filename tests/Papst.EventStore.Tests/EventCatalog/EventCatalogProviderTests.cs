using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Papst.EventStore.EventCatalog;
using Papst.EventStore.Exceptions;
using Shouldly;
using Xunit;

namespace Papst.EventStore.Tests.EventCatalog;

public class EventCatalogProviderTests
{
  private class TestEntity { }
  private class OtherEntity { }

  private static EventCatalogProvider CreateCatalog(Action<EventCatalogRegistration>? configure = null)
  {
    var registration = new EventCatalogRegistration();
    configure?.Invoke(registration);
    return new EventCatalogProvider(new[] { registration });
  }

  // --- EventCatalogRegistration tests (via provider) ---

  [Fact]
  public async Task RegisterEvent_ShouldStoreEventForEntity()
  {
    var provider = CreateCatalog(registration =>
      registration.RegisterEvent<TestEntity>("TestEvent", "A test event", null, new Lazy<string>(() => "{}")));

    var entries = await provider.ListEvents<TestEntity>();

    entries.Count().ShouldBe(1);
    entries.Single().EventName.ShouldBe("TestEvent");
  }

  [Fact]
  public async Task GetEntries_FilterByName_ReturnsMatching()
  {
    var provider = CreateCatalog(registration =>
    {
      registration.RegisterEvent<TestEntity>("EventA", null, null, new Lazy<string>(() => "{}"));
      registration.RegisterEvent<TestEntity>("EventB", null, null, new Lazy<string>(() => "{}"));
    });

    var entries = await provider.ListEvents<TestEntity>(name: "EventA");

    entries.Count().ShouldBe(1);
    entries.Single().EventName.ShouldBe("EventA");
  }

  [Fact]
  public async Task GetEntries_FilterByConstraints_ReturnsMatching()
  {
    var provider = CreateCatalog(registration =>
    {
      registration.RegisterEvent<TestEntity>("EventX", null, new[] { "admin" }, new Lazy<string>(() => "{}"));
      registration.RegisterEvent<TestEntity>("EventY", null, new[] { "user" }, new Lazy<string>(() => "{}"));
    });

    var entries = await provider.ListEvents<TestEntity>(constraints: new[] { "admin" });

    entries.Count().ShouldBe(1);
    entries.Single().EventName.ShouldBe("EventX");
  }

  [Fact]
  public async Task GetEntries_FilterByNameAndConstraints_ReturnsMatching()
  {
    var provider = CreateCatalog(registration =>
    {
      registration.RegisterEvent<TestEntity>("EventA", null, new[] { "admin" }, new Lazy<string>(() => "{}"));
      registration.RegisterEvent<TestEntity>("EventA", null, new[] { "user" }, new Lazy<string>(() => "{}"));
      registration.RegisterEvent<TestEntity>("EventB", null, new[] { "admin" }, new Lazy<string>(() => "{}"));
    });

    var entries = await provider.ListEvents<TestEntity>(name: "EventA", constraints: new[] { "admin" });

    entries.Count().ShouldBe(1);
    entries.Single().EventName.ShouldBe("EventA");
  }

  [Fact]
  public async Task GetEntries_ForUnknownEntity_ReturnsEmpty()
  {
    var provider = CreateCatalog(registration =>
      registration.RegisterEvent<TestEntity>("EventA", null, null, new Lazy<string>(() => "{}")));

    var entries = await provider.ListEvents<OtherEntity>();

    entries.ShouldBeEmpty();
  }

  [Fact]
  public async Task GetDetails_ReturnsSchemaAndDescription()
  {
    const string schema = """{"type":"object","properties":{"id":{"type":"string"}}}""";
    var provider = CreateCatalog(registration =>
      registration.RegisterEvent<TestEntity>("DetailedEvent", "Has a schema", new[] { "v1" }, new Lazy<string>(() => schema)));

    var details = await provider.GetEventDetails("DetailedEvent");

    details.ShouldNotBeNull();
    details!.EventName.ShouldBe("DetailedEvent");
    details.Description.ShouldBe("Has a schema");
    details.Constraints.ShouldBe(new[] { "v1" });
    details.JsonSchema.ShouldBe(schema);
  }

  [Fact]
  public async Task GetDetails_UnknownEvent_ReturnsNull()
  {
    var provider = CreateCatalog();

    var details = await provider.GetEventDetails("NonExistent");

    details.ShouldBeNull();
  }

  [Fact]
  public async Task GetDetails_LazySchemaEvaluatedOnAccess()
  {
    bool evaluated = false;
    var lazySchema = new Lazy<string>(() =>
    {
      evaluated = true;
      return """{"type":"object"}""";
    });
    var provider = CreateCatalog(registration =>
      registration.RegisterEvent<TestEntity>("LazyEvent", null, null, lazySchema));

    evaluated.ShouldBeFalse();

    var details = await provider.GetEventDetails("LazyEvent");

    evaluated.ShouldBeTrue();
    details.ShouldNotBeNull();
    details.JsonSchema.ShouldBe("""{"type":"object"}""");
  }

  // --- EventCatalogProvider delegation tests ---

  [Fact]
  public async Task ListEvents_DelegatesToRegistrations()
  {
    var registration = new EventCatalogRegistration();
    registration.RegisterEvent<TestEntity>("Evt1", "desc", null, new Lazy<string>(() => "{}"));
    var provider = new EventCatalogProvider(new[] { registration });

    var entries = await provider.ListEvents<TestEntity>();

    entries.Count().ShouldBe(1);
    var entry = entries.Single();
    entry.EventName.ShouldBe("Evt1");
    entry.Description.ShouldBe("desc");
    entry.Constraints.ShouldBeNull();
  }

  [Fact]
  public async Task GetEventDetails_DelegatesToRegistrations()
  {
    var registration = new EventCatalogRegistration();
    registration.RegisterEvent<TestEntity>("Evt2", "desc2", new[] { "c1" }, new Lazy<string>(() => """{"schema":true}"""));
    var provider = new EventCatalogProvider(new[] { registration });

    var details = await provider.GetEventDetails("Evt2");

    details.ShouldNotBeNull();
    details!.EventName.ShouldBe("Evt2");
    details.Description.ShouldBe("desc2");
    details.Constraints.ShouldBe(new[] { "c1" });
    details.JsonSchema.ShouldBe("""{"schema":true}""");
  }

  [Fact]
  public async Task ListEvents_CombinesMultipleRegistrations()
  {
    var reg1 = new EventCatalogRegistration();
    reg1.RegisterEvent<TestEntity>("FromReg1", null, null, new Lazy<string>(() => "{}"));

    var reg2 = new EventCatalogRegistration();
    reg2.RegisterEvent<TestEntity>("FromReg2", null, null, new Lazy<string>(() => "{}"));

    var provider = new EventCatalogProvider(new IEventCatalogRegistration[] { reg1, reg2 });

    var entries = await provider.ListEvents<TestEntity>();

    entries.Count().ShouldBe(2);
    entries.Select(e => e.EventName).OrderBy(name => name).ToArray().ShouldBe(new[] { "FromReg1", "FromReg2" });
  }

  // --- Entity-scoped GetEventDetails tests ---

  [Fact]
  public async Task GetEventDetails_EntityScoped_ReturnsDetailsForEntity()
  {
    var provider = CreateCatalog(registration =>
    {
      registration.RegisterEvent<TestEntity>("SharedEvent", "Test version", new[] { "test" }, new Lazy<string>(() => """{"entity":"test"}"""));
      registration.RegisterEvent<OtherEntity>("SharedEvent", "Other version", new[] { "other" }, new Lazy<string>(() => """{"entity":"other"}"""));
    });

    var testDetails = await provider.GetEventDetails<TestEntity>("SharedEvent");
    var otherDetails = await provider.GetEventDetails<OtherEntity>("SharedEvent");

    testDetails.ShouldNotBeNull();
    testDetails!.Description.ShouldBe("Test version");
    testDetails.JsonSchema.ShouldBe("""{"entity":"test"}""");

    otherDetails.ShouldNotBeNull();
    otherDetails!.Description.ShouldBe("Other version");
    otherDetails.JsonSchema.ShouldBe("""{"entity":"other"}""");
  }

  [Fact]
  public async Task GetEventDetails_EntityScoped_UnknownEntity_ReturnsNull()
  {
    var provider = CreateCatalog(registration =>
      registration.RegisterEvent<TestEntity>("SomeEvent", null, null, new Lazy<string>(() => "{}")));

    var details = await provider.GetEventDetails<OtherEntity>("SomeEvent");

    details.ShouldBeNull();
  }

  [Fact]
  public async Task GetEventDetails_DuplicateEventNamesAcrossEntities_GlobalThrowsAmbiguousException()
  {
    var provider = CreateCatalog(registration =>
    {
      registration.RegisterEvent<TestEntity>("DuplicateName", "First", null, new Lazy<string>(() => """{"first":true}"""));
      registration.RegisterEvent<OtherEntity>("DuplicateName", "Second", null, new Lazy<string>(() => """{"second":true}"""));
    });

    Func<Task> act = async () => _ = await provider.GetEventDetails("DuplicateName");

    var exception = await Should.ThrowAsync<EventCatalogAmbiguousEventException>(act);
    exception.Message.ShouldContain("DuplicateName");
  }

  [Fact]
  public async Task GetEventDetails_DuplicateEventNamesAcrossRegistrations_GlobalThrowsAmbiguousException()
  {
    var reg1 = new EventCatalogRegistration();
    reg1.RegisterEvent<TestEntity>("DuplicateName", "First", null, new Lazy<string>(() => """{"first":true}"""));

    var reg2 = new EventCatalogRegistration();
    reg2.RegisterEvent<OtherEntity>("DuplicateName", "Second", null, new Lazy<string>(() => """{"second":true}"""));

    var provider = new EventCatalogProvider(new IEventCatalogRegistration[] { reg1, reg2 });

    Func<Task> act = async () => _ = await provider.GetEventDetails("DuplicateName");

    var exception = await Should.ThrowAsync<EventCatalogAmbiguousEventException>(act);
    exception.Message.ShouldContain("DuplicateName");
  }
}
