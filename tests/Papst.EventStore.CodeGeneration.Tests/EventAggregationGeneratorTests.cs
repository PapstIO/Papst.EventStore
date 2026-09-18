using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Papst.EventStore.CodeGeneration.Tests;

public class EventAggregationGeneratorTests
{
  private const string Prelude = @"
using System;
using System.Collections.Generic;
using Papst.EventStore.Aggregation;
using Papst.EventStore.Aggregation.EventRegistration;

namespace TestApp
{
  public class SubEntity { public string? Name2 { get; set; } }
  public class Item { public Guid Id { get; set; } public string? Value { get; set; } }
  public class Entity : Papst.EventStore.IEntity
  {
    public ulong Version { get; set; }
    public string? Name { get; set; }
    public string? Nick { get; set; }
    public int Count { get; set; }
    public SubEntity Child { get; set; } = new();
    public Dictionary<Guid, Item> Items { get; set; } = new();
    public List<Item> ItemsList { get; set; } = new();
  }
  public class StampedEntity : Papst.EventStore.IEntity
  {
    public ulong Version { get; set; }
    [AggregationContextStamp(AggregationContextValue.StreamId, OnEveryEvent = false)] public Guid Id { get; set; }
    [AggregationContextStamp(AggregationContextValue.StreamCreated, OnEveryEvent = false)] public DateTimeOffset Created { get; set; }
    [AggregationContextStamp(AggregationContextValue.EventTime)] public DateTimeOffset Updated { get; set; }
    public string? Name { get; set; }
  }
  public class BadStampEntity : Papst.EventStore.IEntity
  {
    public ulong Version { get; set; }
    [AggregationContextStamp(AggregationContextValue.EventTime)] public int Updated { get; set; }
    public string? Name { get; set; }
  }
";

  private static (string? aggregators, string? registration, GeneratorDriverRunResult run, Compilation output) Generate(string body)
  {
    Compilation input = CreateCompilation(Prelude + body + "\n}\n");
    GeneratorDriver driver = CSharpGeneratorDriver.Create(new EventRegistrationIncrementalCodeGenerator());
    driver = driver.RunGeneratorsAndUpdateCompilation(input, out Compilation output, out _);
    GeneratorDriverRunResult run = driver.GetRunResult();

    string? SourceFor(string hint) => run.Results[0].GeneratedSources
      .Where(s => s.HintName == hint)
      .Select(s => s.SourceText.ToString())
      .FirstOrDefault();

    return (SourceFor("EventAggregators.g.cs"), SourceFor("EventRegistration.g.cs"), run, output);
  }

  private static Compilation CreateCompilation(string source)
  {
    var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
      .Split(Path.PathSeparator)
      .Where(p => p.Length > 0)
      .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p));

    return CSharpCompilation.Create("compilation",
      new[] { CSharpSyntaxTree.ParseText(source) },
      references,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
  }

  private static void ShouldCompileClean(Compilation output)
    => output.GetDiagnostics()
        .Where(d => d.Severity == DiagnosticSeverity.Error)
        .Select(d => d.ToString())
        .ShouldBeEmpty();

  [Fact]
  public void GeneratesRootAggregator_WithSkipNullByDefault()
  {
    var (aggregators, registration, _, output) = Generate(@"
    [EventAggregation<Entity>]
    public record NameChanged(string? Name);");

    aggregators!.ShouldNotBeNull();
    aggregators!.ShouldContain("class NameChanged_Entity_GeneratedAggregator");
    aggregators!.ShouldContain("var target = entity;");
    aggregators!.ShouldContain("if (evt.Name is not null) { target.Name = evt.Name; }");
    registration.ShouldNotBeNull();
    registration!.ShouldContain("IEventAggregator<global::TestApp.Entity, global::TestApp.NameChanged>");
    ShouldCompileClean(output);
  }

  [Fact]
  public void SkipNullValuesFalse_AssignsUnconditionally()
  {
    var (aggregators, _, _, output) = Generate(@"
    [EventAggregation<Entity>(SkipNullValues = false)]
    public record NameForced(string? Name);");

    aggregators!.ShouldContain("target.Name = evt.Name;");
    aggregators!.ShouldNotContain("if (evt.Name is not null)");
    ShouldCompileClean(output);
  }

  [Fact]
  public void SkipWhenNullAttribute_OverridesGlobalSetting()
  {
    var (aggregators, _, _, output) = Generate(@"
    [EventAggregation<Entity>]
    public record Mixed(string? Name, [property: SkipWhenNull(false)] string? Nick);");

    aggregators!.ShouldContain("if (evt.Name is not null) { target.Name = evt.Name; }");
    aggregators!.ShouldContain("target.Nick = evt.Nick;");
    ShouldCompileClean(output);
  }

  [Fact]
  public void NonNullableValueType_IsAlwaysAssigned()
  {
    var (aggregators, _, _, output) = Generate(@"
    [EventAggregation<Entity>]
    public record CountSet(int Count);");

    aggregators!.ShouldContain("target.Count = evt.Count;");
    ShouldCompileClean(output);
  }

  [Fact]
  public void AggregationIgnore_ExcludesProperty()
  {
    var (aggregators, _, _, output) = Generate(@"
    [EventAggregation<Entity>]
    public record WithIgnored(string? Name, [property: AggregationIgnore] string? Nick);");

    aggregators!.ShouldContain("if (evt.Name is not null) { target.Name = evt.Name; }");
    aggregators!.ShouldNotContain("target.Nick");
    aggregators!.ShouldNotContain("evt.Nick");
    ShouldCompileClean(output);
  }

  [Fact]
  public void AggregationProperty_RemapsTargetName()
  {
    var (aggregators, _, _, output) = Generate(@"
    [EventAggregation<Entity>]
    public record Renamed([property: AggregationProperty(nameof(Entity.Nick))] string? DisplayName);");

    aggregators!.ShouldContain("if (evt.DisplayName is not null) { target.Nick = evt.DisplayName; }");
    ShouldCompileClean(output);
  }

  [Fact]
  public void AggregationProperty_RemapsValueTypeTarget()
  {
    var (aggregators, _, _, output) = Generate(@"
    [EventAggregation<Entity>]
    public record CountRenamed([property: AggregationProperty(nameof(Entity.Count))] int Amount);");

    aggregators!.ShouldContain("target.Count = evt.Amount;");
    ShouldCompileClean(output);
  }

  [Fact]
  public void NestedPropertyPath_NavigatesAndInstantiates()
  {
    var (aggregators, _, _, output) = Generate(@"
    [EventAggregation<Entity>(PropertyPath = nameof(Entity.Child))]
    public record ChildRenamed(string? Name2);");

    aggregators!.ShouldContain("entity.Child ??= new global::TestApp.SubEntity();");
    aggregators!.ShouldContain("var target = entity.Child;");
    aggregators!.ShouldContain("if (evt.Name2 is not null) { target.Name2 = evt.Name2; }");
    ShouldCompileClean(output);
  }

  [Fact]
  public void DictionaryPropertyPath_UpsertsByKey()
  {
    var (aggregators, _, _, output) = Generate(@"
    [EventAggregation<Entity>(PropertyPath = nameof(Entity.Items))]
    public record ItemUpdated([property: AggregationDictionaryKey] Guid Id, string? Value);");

    aggregators!.ShouldContain("entity.Items ??= new global::System.Collections.Generic.Dictionary<global::System.Guid, global::TestApp.Item>();");
    aggregators!.ShouldContain("if (!entity.Items.TryGetValue(evt.Id, out var target))");
    aggregators!.ShouldContain("entity.Items[evt.Id] = target;");
    aggregators!.ShouldContain("if (evt.Value is not null) { target.Value = evt.Value; }");
    // the key property must not be mapped as a normal value
    aggregators!.ShouldNotContain("target.Id = evt.Id;");
    ShouldCompileClean(output);
  }

  [Fact]
  public void CollectionPropertyPath_UpsertsBySearchKey()
  {
    var (aggregators, _, _, output) = Generate(@"
    [EventAggregation<Entity>(PropertyPath = nameof(Entity.ItemsList))]
    public record ItemAppended([property: AggregationCollectionKey(""Id"")] Guid ItemId, string? Value);");

    aggregators!.ShouldContain("entity.ItemsList ??= new global::System.Collections.Generic.List<global::TestApp.Item>();");
    aggregators!.ShouldContain("global::System.Linq.Enumerable.FirstOrDefault(entity.ItemsList");
    aggregators!.ShouldContain("target = new global::TestApp.Item();");
    aggregators!.ShouldContain("target.Id = evt.ItemId;");
    aggregators!.ShouldContain("entity.ItemsList.Add(target);");
    aggregators!.ShouldContain("if (evt.Value is not null) { target.Value = evt.Value; }");
    ShouldCompileClean(output);
  }

  [Fact]
  public void ContextStamp_EmitsEveryEventAndCreateOnlyStamps()
  {
    var (aggregators, _, _, output) = Generate(@"
    [EventAggregation<StampedEntity>]
    public record ThingRenamed(string? Name);");

    aggregators!.ShouldContain("class ThingRenamed_StampedEntity_GeneratedAggregator");
    // every-event stamp is unconditional
    aggregators!.ShouldContain("entity.Updated = ctx.EventTime;");
    // create-only stamps are guarded by CurrentVersion == 0
    aggregators!.ShouldContain("if (ctx.CurrentVersion == 0)");
    aggregators!.ShouldContain("entity.Id = ctx.StreamId;");
    aggregators!.ShouldContain("entity.Created = ctx.StreamCreated;");
    ShouldCompileClean(output);
  }

  [Fact]
  public void ContextStamp_NotAssignableTarget_SkipsAndReportsDiagnostic()
  {
    var (aggregators, _, run, output) = Generate(@"
    [EventAggregation<BadStampEntity>]
    public record BadRenamed(string? Name);");

    run.Diagnostics.Any(d => d.Id == "EVTSRC0005").ShouldBeTrue();
    aggregators!.ShouldNotContain("entity.Updated = ctx.EventTime;");
    ShouldCompileClean(output);
  }

  [Fact]
  public void CollectionRemoveByKey_RemovesMatchingItems()
  {
    var (aggregators, _, _, output) = Generate(@"
    [EventAggregation<Entity>(PropertyPath = nameof(Entity.ItemsList), Mode = AggregationMode.RemoveByKey)]
    public record ItemRemoved([property: AggregationCollectionKey(""Id"")] Guid ItemId);");

    aggregators!.ShouldContain("entity.ItemsList ??= new global::System.Collections.Generic.List<global::TestApp.Item>();");
    aggregators!.ShouldContain("global::System.Linq.Enumerable.Where(entity.ItemsList");
    aggregators!.ShouldContain("entity.ItemsList.Remove(__r);");
    // upsert path must not be emitted
    aggregators!.ShouldNotContain("FirstOrDefault");
    aggregators!.ShouldNotContain("entity.ItemsList.Add(");
    ShouldCompileClean(output);
  }

  [Fact]
  public void DictionaryRemoveByKey_RemovesEntryAndSkipsMapping()
  {
    var (aggregators, _, _, output) = Generate(@"
    [EventAggregation<Entity>(PropertyPath = nameof(Entity.Items), Mode = AggregationMode.RemoveByKey)]
    public record EntryRemoved([property: AggregationDictionaryKey] Guid Id, string? Value);");

    aggregators!.ShouldContain("entity.Items.Remove(evt.Id);");
    aggregators!.ShouldNotContain("TryGetValue");
    // element property mapping is skipped for removals
    aggregators!.ShouldNotContain("target.Value");
    ShouldCompileClean(output);
  }

  [Fact]
  public void RemoveByKey_WithoutKey_ReportsDiagnostic()
  {
    var (aggregators, _, run, output) = Generate(@"
    [EventAggregation<Entity>(PropertyPath = nameof(Entity.ItemsList), Mode = AggregationMode.RemoveByKey)]
    public record BadRemove(string? Value);");

    run.Diagnostics.Any(d => d.Id == "EVTSRC0006").ShouldBeTrue();
    (aggregators is null || !aggregators.Contains("BadRemove_Entity_GeneratedAggregator")).ShouldBeTrue();
    ShouldCompileClean(output);
  }

  [Fact]
  public void ManualAggregatorConflict_SkipsGenerationAndReportsDiagnostic()
  {
    var (aggregators, _, run, output) = Generate(@"
    [EventAggregation<Entity>]
    public record Conflicting(string? Name);

    public class ConflictingAggregator : EventAggregatorBase<Entity, Conflicting>
    {
      public override System.Threading.Tasks.ValueTask<Entity?> ApplyAsync(Conflicting evt, Entity entity, Papst.EventStore.IAggregatorStreamContext ctx)
        => AsTask(entity);
    }");

    run.Diagnostics.Any(d => d.Id == "EVTSRC0003").ShouldBeTrue();
    (aggregators is null || !aggregators.Contains("Conflicting_Entity_GeneratedAggregator")).ShouldBeTrue();
    ShouldCompileClean(output);
  }
}
