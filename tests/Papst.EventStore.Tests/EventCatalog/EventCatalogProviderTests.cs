using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Papst.EventStore.EventCatalog;
using Xunit;

namespace Papst.EventStore.Tests.EventCatalog;

public class EventCatalogProviderTests
{
  private class TestEntity { }
  private class OtherEntity { }

  private static (EventCatalogRegistration registration, EventCatalogProvider provider) CreateCatalog()
  {
    var registration = new EventCatalogRegistration();
    var provider = new EventCatalogProvider(new[] { registration });
    return (registration, provider);
  }

  // --- EventCatalogRegistration tests (via provider) ---

  [Fact]
  public async Task RegisterEvent_ShouldStoreEventForEntity()
  {
    var (registration, provider) = CreateCatalog();

    registration.RegisterEvent<TestEntity>("TestEvent", "A test event", null, new Lazy<string>(() => "{}"));

    var entries = await provider.ListEvents<TestEntity>();

    entries.Should().ContainSingle()
      .Which.EventName.Should().Be("TestEvent");
  }

  [Fact]
  public async Task GetEntries_FilterByName_ReturnsMatching()
  {
    var (registration, provider) = CreateCatalog();

    registration.RegisterEvent<TestEntity>("EventA", null, null, new Lazy<string>(() => "{}"));
    registration.RegisterEvent<TestEntity>("EventB", null, null, new Lazy<string>(() => "{}"));

    var entries = await provider.ListEvents<TestEntity>(name: "EventA");

    entries.Should().ContainSingle()
      .Which.EventName.Should().Be("EventA");
  }

  [Fact]
  public async Task GetEntries_FilterByConstraints_ReturnsMatching()
  {
    var (registration, provider) = CreateCatalog();

    registration.RegisterEvent<TestEntity>("EventX", null, new[] { "admin" }, new Lazy<string>(() => "{}"));
    registration.RegisterEvent<TestEntity>("EventY", null, new[] { "user" }, new Lazy<string>(() => "{}"));

    var entries = await provider.ListEvents<TestEntity>(constraints: new[] { "admin" });

    entries.Should().ContainSingle()
      .Which.EventName.Should().Be("EventX");
  }

  [Fact]
  public async Task GetEntries_FilterByNameAndConstraints_ReturnsMatching()
  {
    var (registration, provider) = CreateCatalog();

    registration.RegisterEvent<TestEntity>("EventA", null, new[] { "admin" }, new Lazy<string>(() => "{}"));
    registration.RegisterEvent<TestEntity>("EventA", null, new[] { "user" }, new Lazy<string>(() => "{}"));
    registration.RegisterEvent<TestEntity>("EventB", null, new[] { "admin" }, new Lazy<string>(() => "{}"));

    var entries = await provider.ListEvents<TestEntity>(name: "EventA", constraints: new[] { "admin" });

    entries.Should().ContainSingle()
      .Which.EventName.Should().Be("EventA");
  }

  [Fact]
  public async Task GetEntries_ForUnknownEntity_ReturnsEmpty()
  {
    var (registration, provider) = CreateCatalog();

    registration.RegisterEvent<TestEntity>("EventA", null, null, new Lazy<string>(() => "{}"));

    var entries = await provider.ListEvents<OtherEntity>();

    entries.Should().BeEmpty();
  }

  [Fact]
  public async Task GetDetails_ReturnsSchemaAndDescription()
  {
    var (registration, provider) = CreateCatalog();
    const string schema = """{"type":"object","properties":{"id":{"type":"string"}}}""";

    registration.RegisterEvent<TestEntity>("DetailedEvent", "Has a schema", new[] { "v1" }, new Lazy<string>(() => schema));

    var details = await provider.GetEventDetails("DetailedEvent");

    details.Should().NotBeNull();
    details!.EventName.Should().Be("DetailedEvent");
    details.Description.Should().Be("Has a schema");
    details.Constraints.Should().BeEquivalentTo(new[] { "v1" });
    details.JsonSchema.Should().Be(schema);
  }

  [Fact]
  public async Task GetDetails_UnknownEvent_ReturnsNull()
  {
    var (_, provider) = CreateCatalog();

    var details = await provider.GetEventDetails("NonExistent");

    details.Should().BeNull();
  }

  [Fact]
  public async Task GetDetails_LazySchemaEvaluatedOnAccess()
  {
    var (registration, provider) = CreateCatalog();
    bool evaluated = false;
    var lazySchema = new Lazy<string>(() =>
    {
      evaluated = true;
      return """{"type":"object"}""";
    });

    registration.RegisterEvent<TestEntity>("LazyEvent", null, null, lazySchema);

    evaluated.Should().BeFalse("schema should not be evaluated at registration time");

    var details = await provider.GetEventDetails("LazyEvent");

    evaluated.Should().BeTrue("schema should be evaluated when details are requested");
    details!.JsonSchema.Should().Be("""{"type":"object"}""");
  }

  // --- EventCatalogProvider delegation tests ---

  [Fact]
  public async Task ListEvents_DelegatesToRegistrations()
  {
    var registration = new EventCatalogRegistration();
    registration.RegisterEvent<TestEntity>("Evt1", "desc", null, new Lazy<string>(() => "{}"));
    var provider = new EventCatalogProvider(new[] { registration });

    var entries = await provider.ListEvents<TestEntity>();

    entries.Should().ContainSingle()
      .Which.Should().BeEquivalentTo(new EventCatalogEntry("Evt1", "desc", null));
  }

  [Fact]
  public async Task GetEventDetails_DelegatesToRegistrations()
  {
    var registration = new EventCatalogRegistration();
    registration.RegisterEvent<TestEntity>("Evt2", "desc2", new[] { "c1" }, new Lazy<string>(() => """{"schema":true}"""));
    var provider = new EventCatalogProvider(new[] { registration });

    var details = await provider.GetEventDetails("Evt2");

    details.Should().NotBeNull();
    details.Should().BeEquivalentTo(new EventCatalogEventDetails("Evt2", "desc2", new[] { "c1" }, """{"schema":true}"""));
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

    entries.Should().HaveCount(2);
    entries.Select(e => e.EventName).Should().BeEquivalentTo("FromReg1", "FromReg2");
  }

  // --- Entity-scoped GetEventDetails tests ---

  [Fact]
  public async Task GetEventDetails_EntityScoped_ReturnsDetailsForEntity()
  {
    var (registration, provider) = CreateCatalog();

    registration.RegisterEvent<TestEntity>("SharedEvent", "Test version", new[] { "test" }, new Lazy<string>(() => """{"entity":"test"}"""));
    registration.RegisterEvent<OtherEntity>("SharedEvent", "Other version", new[] { "other" }, new Lazy<string>(() => """{"entity":"other"}"""));

    var testDetails = await provider.GetEventDetails<TestEntity>("SharedEvent");
    var otherDetails = await provider.GetEventDetails<OtherEntity>("SharedEvent");

    testDetails.Should().NotBeNull();
    testDetails!.Description.Should().Be("Test version");
    testDetails.JsonSchema.Should().Be("""{"entity":"test"}""");

    otherDetails.Should().NotBeNull();
    otherDetails!.Description.Should().Be("Other version");
    otherDetails.JsonSchema.Should().Be("""{"entity":"other"}""");
  }

  [Fact]
  public async Task GetEventDetails_EntityScoped_UnknownEntity_ReturnsNull()
  {
    var (registration, provider) = CreateCatalog();

    registration.RegisterEvent<TestEntity>("SomeEvent", null, null, new Lazy<string>(() => "{}"));

    var details = await provider.GetEventDetails<OtherEntity>("SomeEvent");

    details.Should().BeNull();
  }

  [Fact]
  public async Task GetEventDetails_DuplicateEventNamesAcrossEntities_GlobalReturnsFirst()
  {
    var (registration, provider) = CreateCatalog();

    registration.RegisterEvent<TestEntity>("DuplicateName", "First", null, new Lazy<string>(() => """{"first":true}"""));
    registration.RegisterEvent<OtherEntity>("DuplicateName", "Second", null, new Lazy<string>(() => """{"second":true}"""));

    var details = await provider.GetEventDetails("DuplicateName");

    details.Should().NotBeNull();
    details!.Description.Should().Be("First");
  }
}
