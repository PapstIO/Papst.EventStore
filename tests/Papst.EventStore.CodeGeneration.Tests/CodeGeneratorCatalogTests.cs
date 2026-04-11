using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Shouldly;
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
    diagnostics.ShouldBeEmpty();
    var runResult = driver.GetRunResult();
    runResult.Diagnostics.ShouldBeEmpty();
    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.ShouldContain("AddCodeGeneratedEventCatalog");
    source.ShouldContain("catalog.RegisterEvent<MyCode.UserEntity>(");
    source.ShouldContain("\"UserCreated\"");
    source.ShouldContain("\"A user was created\"");
    source.ShouldContain("new string[] { \"Create\" }");
    source.ShouldContain("\"\"name\"\"");
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
    diagnostics.ShouldBeEmpty();
    var runResult = driver.GetRunResult();
    runResult.Diagnostics.ShouldBeEmpty();
    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.ShouldContain("AddCodeGeneratedEventCatalog");
    source.ShouldContain("catalog.RegisterEvent<MyCode.FooEntity>(");
    source.ShouldContain("\"FooEvent\"");
    source.ShouldContain("\"\"value\"\"");
    source.ShouldContain("\"\"integer\"\"");
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
    diagnostics.ShouldBeEmpty();
    var runResult = driver.GetRunResult();
    runResult.Diagnostics.ShouldBeEmpty();
    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.ShouldContain("\"\"name\"\":{\"\"type\"\":\"\"string\"\"}");
    source.ShouldContain("\"\"count\"\":{\"\"type\"\":\"\"integer\"\"}");
    source.ShouldContain("\"\"isActive\"\":{\"\"type\"\":\"\"boolean\"\"}");
    source.ShouldContain("\"\"status\"\":{\"\"type\"\":\"\"string\"\",\"\"enum\"\":[\"\"Active\"\",\"\"Inactive\"\"]}");
    source.ShouldContain("\"\"tags\"\":{\"\"type\"\":\"\"array\"\",\"\"items\"\":{\"\"type\"\":\"\"string\"\"}}");
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
    diagnostics.ShouldBeEmpty();
    var runResult = driver.GetRunResult();
    runResult.Diagnostics.ShouldBeEmpty();
    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.ShouldContain("catalog.RegisterEvent<MyCode.BarEntity>(\"BarEvent\", \"Bar happened\", new string[] { \"Read\", \"Write\" },");
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
    diagnostics.ShouldBeEmpty();
    var runResult = driver.GetRunResult();
    runResult.Diagnostics.ShouldBeEmpty();
    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.ShouldNotContain("AddCodeGeneratedEventCatalog");
    source.ShouldContain("AddCodeGeneratedEvents");
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
    diagnostics.ShouldBeEmpty();
    var runResult = driver.GetRunResult();
    runResult.Diagnostics.ShouldBeEmpty();
    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.ShouldContain("catalog.RegisterEvent<MyCode.MyEntity>(\"SimpleEvent\", null, null,");
  }

  [Fact]
  public void TestQualifiedGenericEventNameAttributeGeneratesCatalog()
  {
    var generator = new EventRegistrationIncrementalCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
  public class Program { public static void Main(string[] args) {} }
  public class OrderEntity {}
  [Papst.EventStore.Aggregation.EventRegistration.EventName<OrderEntity>(""OrderCreated"")]
  public record OrderCreatedEvent(string Number);
}
");
    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);
    diagnostics.ShouldBeEmpty();
    var runResult = driver.GetRunResult();
    runResult.Diagnostics.ShouldBeEmpty();
    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.ShouldContain("catalog.RegisterEvent<MyCode.OrderEntity>(\"OrderCreated\", null, null,");
  }

  [Fact]
  public void TestGlobalQualifiedGenericEventNameAttributeGeneratesCatalog()
  {
    var generator = new EventRegistrationIncrementalCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
  public class Program { public static void Main(string[] args) {} }
  public class InvoiceEntity {}
  [global::Papst.EventStore.Aggregation.EventRegistration.EventName<InvoiceEntity>(""InvoiceIssued"")]
  public record InvoiceIssuedEvent(string Number);
}
");
    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);
    diagnostics.ShouldBeEmpty();
    var runResult = driver.GetRunResult();
    runResult.Diagnostics.ShouldBeEmpty();
    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.ShouldContain("catalog.RegisterEvent<MyCode.InvoiceEntity>(\"InvoiceIssued\", null, null,");
  }
}
