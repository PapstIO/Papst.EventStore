using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Xunit;

namespace Papst.EventStore.CodeGeneration.Tests;

public class CodeGeneratorCatalogTests : CodeGeneratorTestBase
{
  [Fact]
  public void TestGenericEventNameAttributeGeneratesCatalog()
  {
    var generator = new EventRegistrationIncrementalCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
  public class Program { public static void Main(string[] args) {} }
  public class UserEntity {}
  [EventName<UserEntity>(""UserCreated"", Description = ""A user was created"", Constraints = new[] { ""Create"" })]
  public record UserCreatedEvent(string Name);
}
");
    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);
    diagnostics.Should().BeEmpty();
    var runResult = driver.GetRunResult();
    runResult.Diagnostics.Should().BeEmpty();
    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.Should().Contain("AddCodeGeneratedEventCatalog");
    source.Should().Contain("catalog.RegisterEvent<MyCode.UserEntity>(");
    source.Should().Contain("\"UserCreated\"");
    source.Should().Contain("\"A user was created\"");
    source.Should().Contain("new string[] { \"Create\" }");
    source.Should().Contain("\"\"name\"\"");
  }

  [Fact]
  public void TestAggregatorBasedCatalogGeneration()
  {
    var generator = new EventRegistrationIncrementalCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
  public class Program { public static void Main(string[] args) {} }
  [EventName(""FooEvent"")]
  public record FooEvent(int Value);
  public record FooEntity();
  public class FooAggregator : EventAggregatorBase<FooEntity, FooEvent>
  {
    public override Task<FooEntity?> ApplyAsync(FooEvent evt, FooEntity entity, IAggregatorStreamContext ctx)
    { throw new NotImplementedException(); }
  }
}
");
    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);
    diagnostics.Should().BeEmpty();
    var runResult = driver.GetRunResult();
    runResult.Diagnostics.Should().BeEmpty();
    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.Should().Contain("AddCodeGeneratedEventCatalog");
    source.Should().Contain("catalog.RegisterEvent<MyCode.FooEntity>(");
    source.Should().Contain("\"FooEvent\"");
    source.Should().Contain("\"\"value\"\"");
    source.Should().Contain("\"\"integer\"\"");
  }

  [Fact]
  public void TestCatalogJsonSchemaGeneration()
  {
    var generator = new EventRegistrationIncrementalCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
  public class Program { public static void Main(string[] args) {} }
  public class MyEntity {}
  public enum Status { Active, Inactive }
  [EventName<MyEntity>(""ComplexEvent"")]
  public record ComplexEvent(string Name, int Count, bool IsActive, Status Status, string[] Tags);
}
");
    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);
    diagnostics.Should().BeEmpty();
    var runResult = driver.GetRunResult();
    runResult.Diagnostics.Should().BeEmpty();
    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.Should().Contain("\"\"name\"\":{\"\"type\"\":\"\"string\"\"}");
    source.Should().Contain("\"\"count\"\":{\"\"type\"\":\"\"integer\"\"}");
    source.Should().Contain("\"\"isActive\"\":{\"\"type\"\":\"\"boolean\"\"}");
    source.Should().Contain("\"\"status\"\":{\"\"type\"\":\"\"string\"\",\"\"enum\"\":[\"\"Active\"\",\"\"Inactive\"\"]}");
    source.Should().Contain("\"\"tags\"\":{\"\"type\"\":\"\"array\"\",\"\"items\"\":{\"\"type\"\":\"\"string\"\"}}");
  }

  [Fact]
  public void TestDescriptionAndConstraintsOnNonGenericAttribute()
  {
    var generator = new EventRegistrationIncrementalCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
  public class Program { public static void Main(string[] args) {} }
  [EventName(""BarEvent"", Description = ""Bar happened"", Constraints = new[] { ""Read"", ""Write"" })]
  public record BarEvent(string Data);
  public record BarEntity();
  public class BarAggregator : EventAggregatorBase<BarEntity, BarEvent>
  {
    public override Task<BarEntity?> ApplyAsync(BarEvent evt, BarEntity entity, IAggregatorStreamContext ctx)
    { throw new NotImplementedException(); }
  }
}
");
    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);
    diagnostics.Should().BeEmpty();
    var runResult = driver.GetRunResult();
    runResult.Diagnostics.Should().BeEmpty();
    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.Should().Contain("catalog.RegisterEvent<MyCode.BarEntity>(\"BarEvent\", \"Bar happened\", new string[] { \"Read\", \"Write\" },");
  }

  [Fact]
  public void TestNoCatalogWithoutEntityAssociation()
  {
    var generator = new EventRegistrationIncrementalCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
  public class Program { public static void Main(string[] args) {} }
  [EventName(""OrphanEvent"")]
  public record OrphanEvent(string Value);
}
");
    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);
    diagnostics.Should().BeEmpty();
    var runResult = driver.GetRunResult();
    runResult.Diagnostics.Should().BeEmpty();
    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.Should().NotContain("AddCodeGeneratedEventCatalog");
    source.Should().Contain("AddCodeGeneratedEvents");
  }

  [Fact]
  public void TestCatalogWithNullDescriptionAndConstraints()
  {
    var generator = new EventRegistrationIncrementalCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
  public class Program { public static void Main(string[] args) {} }
  public class MyEntity {}
  [EventName<MyEntity>(""SimpleEvent"")]
  public record SimpleEvent(string Value);
}
");
    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);
    diagnostics.Should().BeEmpty();
    var runResult = driver.GetRunResult();
    runResult.Diagnostics.Should().BeEmpty();
    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.Should().Contain("catalog.RegisterEvent<MyCode.MyEntity>(\"SimpleEvent\", null, null,");
  }
}
