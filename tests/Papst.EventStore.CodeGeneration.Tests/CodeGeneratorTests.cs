
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Papst.EventStore.CodeGeneration.Tests;

public class CodeGeneratorTests
{
  [Fact]
  public void TestFindsNamespaceWithEntryPoint()
  {
    var generator = new EventRegistrationIncrementalCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
    public class Program
    {
        public static void Main(string[] args)
        {
        }    
    }
    [EventName(""Foo"")]
    public record Foo {}
}
");

    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

    diagnostics.Should().BeEmpty();
    outputCompilation.SyntaxTrees.Should().HaveCount(2);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Should().HaveCount(1);
    runResult.Diagnostics.Should().BeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.Should().Contain("namespace MyCode");
  }

  [Fact]
  public void TestFindsNamespaceWithoutEntryPoint()
  {
    var generator = new EventRegistrationIncrementalCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
  [EventName(""Foo"")]
  public record Foo {}
}
");

    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

    diagnostics.Should().BeEmpty();
    outputCompilation.SyntaxTrees.Should().HaveCount(2);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Should().HaveCount(1);
    runResult.Diagnostics.Should().BeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    //source.Should().Contain("namespace MyCode");
    source.Should().Contain("namespace compilation", "This does not return MyCode, because the compiling process is called compilation!");
  }

  [Fact]
  public void TestCodeGeneration()
  {
    var generator = new EventRegistrationIncrementalCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
    public class Program
    {
        public static void Main(string[] args)
        {
        }
    }
  [EventName(""Foo"", true)]
  public class TestEventFoo
  {

  }
}
");

    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

    diagnostics.Should().BeEmpty();
    outputCompilation.SyntaxTrees.Should().HaveCount(2);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Should().HaveCount(1);
    runResult.Diagnostics.Should().BeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.Should().Contain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");
  }

  [Fact]
  public void TestCodeGenerationFullAttributeName()
  {
    var generator = new EventRegistrationIncrementalCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
    public class Program
    {
        public static void Main(string[] args)
        {
        }
    }
  [EventNameAttribute(Name = ""Foo"", IsWriteName = true)]
  public class TestEventFoo
  {

  }
}
");

    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

    diagnostics.Should().BeEmpty();
    outputCompilation.SyntaxTrees.Should().HaveCount(2);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Should().HaveCount(1);
    runResult.Diagnostics.Should().BeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.Should().Contain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");

  }

  [Fact]
  public void TestFileScopedNamespaceEventRegistration()
  {
    var generator = new EventRegistrationIncrementalCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode;
public class Program
{
  public static void Main(string[] args)
  {
  }
}
[EventName(""Foo"", true)]
public class TestEventFoo
{

}
");

    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

    diagnostics.Should().BeEmpty();
    outputCompilation.SyntaxTrees.Should().HaveCount(2);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Should().HaveCount(1);
    runResult.Diagnostics.Should().BeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.Should().Contain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");

  }

  [Fact]
  public void TestEventWithTwoDescriptors()
  {
    var generator = new EventRegistrationIncrementalCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
    public class Program
    {
        public static void Main(string[] args)
        {
        }
    }
  
  [EventName(Name = ""FooOld"", false)]
  [EventName(""Foo"", true)]
  public class TestEventFoo
  {

  }
}
");

    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

    diagnostics.Should().BeEmpty();
    outputCompilation.SyntaxTrees.Should().HaveCount(2);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Should().HaveCount(1);
    runResult.Diagnostics.Should().BeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.Should().Contain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.EventRegistration.EventAttributeDescriptor(\"FooOld\", false), new Papst.EventStore.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");

  }

  [Fact]
  public void TestEventWithReadOnlyDescriptor()
  {
    var generator = new EventRegistrationIncrementalCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
    public class Program
    {
        public static void Main(string[] args)
        {
        }
    }
  
  [EventName(""FooOld"", false)]
  [EventName(""Foo"", true)]
  public class TestEventFoo
  {

  }
}
");

    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

    diagnostics.Should().BeEmpty();
    outputCompilation.SyntaxTrees.Should().HaveCount(2);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Should().HaveCount(1);
    runResult.Diagnostics.Should().BeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.Should().Contain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.EventRegistration.EventAttributeDescriptor(\"FooOld\", false), new Papst.EventStore.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");

  }

  [Fact]
  public void TestRecordCodeGeneration()
  {
    var generator = new EventRegistrationIncrementalCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
    public class Program
    {
        public static void Main(string[] args)
        {
        }
    }
  [EventName(""Foo"", true)]
  public record TestEventFoo
  {

  }
}
");

    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

    diagnostics.Should().BeEmpty();
    outputCompilation.SyntaxTrees.Should().HaveCount(2);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Should().HaveCount(1);
    runResult.Diagnostics.Should().BeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.Should().Contain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");

  }

  [Fact]
  public void TestAddsEventAggregatorToSource()
  {
    var generator = new EventRegistrationIncrementalCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
    public class Program
    {
        public static void Main(string[] args)
        {
        }
    }
  [EventName(""Foo"", true)]
  public class TestEventFoo
  {
    
  }

  public record FooEntity {}

  public class TestEventFooAgg : EventAggregatorBase<FooEntity, TestEventFoo>
  {
    public override Task<FooEntity?> ApplyAsync(MyEventSourcingEvent evt, FooEntity entity, IAggregatorStreamContext ctx)
    {
      throw new NotImplementedException();
    }
  }
");

    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

    diagnostics.Should().BeEmpty();
    outputCompilation.SyntaxTrees.Should().HaveCount(2);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Should().HaveCount(1);
    runResult.Diagnostics.Should().BeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.Should().Contain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");
    source.Should().Contain("services.AddTransient<Papst.EventStore.Aggregation.IEventAggregator<MyCode.FooEntity, MyCode.TestEventFoo>, MyCode.TestEventFooAgg");
  }

  [Fact]
  public void TestAddsEventWithMultipleImplementationsAggregatorToSource()
  {
    var generator = new EventRegistrationIncrementalCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
    public class Program
    {
        public static void Main(string[] args)
        {
        }
    }
  [EventName(""Foo"", true)]
  public class TestEventFoo
  {
  }

  public class TestEventFoo2
  {}

  public record FooEntity {}

  public class TestEventFooAgg : EventAggregatorBase<FooEntity, TestEventFoo>, EventAggregatorBase<FooEntity, TestEventFoo2>
  {
    public override Task<FooEntity?> ApplyAsync(TestEventFoo evt, FooEntity entity, IAggregatorStreamContext ctx)
    {
      throw new NotImplementedException();
    }
    public override Task<FooEntity?> ApplyAsync(TestEventFoo evt, FooEntity entity, IAggregatorStreamContext ctx)
    {
      throw new NotImplementedException();
    }
  }
");

    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

    diagnostics.Should().BeEmpty();
    outputCompilation.SyntaxTrees.Should().HaveCount(2);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Should().HaveCount(1);
    runResult.Diagnostics.Should().BeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.Should().Contain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");
    source.Should().Contain("services.AddTransient<Papst.EventStore.Aggregation.IEventAggregator<MyCode.FooEntity, MyCode.TestEventFoo>, MyCode.TestEventFooAgg");
    source.Should().Contain("services.AddTransient<Papst.EventStore.Aggregation.IEventAggregator<MyCode.FooEntity, MyCode.TestEventFoo2>, MyCode.TestEventFooAgg");
  }

  [Fact]
  public void TestAddsInternalAggregatorsToRegistration()
  {
    var generator = new EventRegistrationIncrementalCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
    public class Program
    {
        public static void Main(string[] args)
        {
        }
    }
  [EventName(""Foo"", true)]
  public class TestEventFoo
  {
    
  }

  public record FooEntity {}

  internal class TestEventFooAgg : EventAggregatorBase<FooEntity, TestEventFoo>
  {
    public override Task<FooEntity?> ApplyAsync(MyEventSourcingEvent evt, FooEntity entity, IAggregatorStreamContext ctx)
    {
      throw new NotImplementedException();
    }
  }
");

    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

    diagnostics.Should().BeEmpty();
    outputCompilation.SyntaxTrees.Should().HaveCount(2);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Should().HaveCount(1);
    runResult.Diagnostics.Should().BeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.Should().Contain("services.AddTransient<Papst.EventStore.Aggregation.IEventAggregator<MyCode.FooEntity, MyCode.TestEventFoo>, MyCode.TestEventFooAgg");
  }

  [Fact]
  public void TestAddsDiagnosticsOnEmptyCompilation()
  {
    var generator = new EventRegistrationIncrementalCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
    public class Program
    {
        public static void Main(string[] args)
        {
        }
    }
}");
    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

    diagnostics.Should().HaveCount(1);
    diagnostics.First().Id.Should().Be("EVTSRC0002");
  }

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

  private Compilation CreateCompilation(string source)
  {
    return CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source) },
                new[] { MetadataReference.CreateFromFile(typeof(System.Reflection.Binder).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.ConsoleApplication));
  }

}
