
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Reflection;
using Shouldly;
using Xunit;

namespace Papst.EventStore.CodeGeneration.Tests;

public class CodeGeneratorTests : CodeGeneratorTestBase
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

    diagnostics.ShouldBeEmpty();
    outputCompilation.SyntaxTrees.Count().ShouldBe(3);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Length.ShouldBe(2);
    runResult.Diagnostics.ShouldBeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.ShouldContain("namespace MyCode");
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

    diagnostics.ShouldBeEmpty();
    outputCompilation.SyntaxTrees.Count().ShouldBe(3);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Length.ShouldBe(2);
    runResult.Diagnostics.ShouldBeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    //source.Should().Contain("namespace MyCode");
    source.ShouldContain("namespace compilation");
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

    diagnostics.ShouldBeEmpty();
    outputCompilation.SyntaxTrees.Count().ShouldBe(3);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Length.ShouldBe(2);
    runResult.Diagnostics.ShouldBeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.ShouldContain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");
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

    diagnostics.ShouldBeEmpty();
    outputCompilation.SyntaxTrees.Count().ShouldBe(3);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Length.ShouldBe(2);
    runResult.Diagnostics.ShouldBeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.ShouldContain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");

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

    diagnostics.ShouldBeEmpty();
    outputCompilation.SyntaxTrees.Count().ShouldBe(3);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Length.ShouldBe(2);
    runResult.Diagnostics.ShouldBeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.ShouldContain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");

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

    diagnostics.ShouldBeEmpty();
    outputCompilation.SyntaxTrees.Count().ShouldBe(3);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Length.ShouldBe(2);
    runResult.Diagnostics.ShouldBeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.ShouldContain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.EventRegistration.EventAttributeDescriptor(\"FooOld\", false), new Papst.EventStore.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");

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

    diagnostics.ShouldBeEmpty();
    outputCompilation.SyntaxTrees.Count().ShouldBe(3);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Length.ShouldBe(2);
    runResult.Diagnostics.ShouldBeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.ShouldContain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.EventRegistration.EventAttributeDescriptor(\"FooOld\", false), new Papst.EventStore.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");

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

    diagnostics.ShouldBeEmpty();
    outputCompilation.SyntaxTrees.Count().ShouldBe(3);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Length.ShouldBe(2);
    runResult.Diagnostics.ShouldBeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.ShouldContain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");

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

    diagnostics.ShouldBeEmpty();
    outputCompilation.SyntaxTrees.Count().ShouldBe(3);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Length.ShouldBe(2);
    runResult.Diagnostics.ShouldBeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.ShouldContain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");
    source.ShouldContain("services.AddTransient<Papst.EventStore.Aggregation.IEventAggregator<MyCode.FooEntity, MyCode.TestEventFoo>, MyCode.TestEventFooAgg");
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

    diagnostics.ShouldBeEmpty();
    outputCompilation.SyntaxTrees.Count().ShouldBe(3);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Length.ShouldBe(2);
    runResult.Diagnostics.ShouldBeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.ShouldContain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");
    source.ShouldContain("services.AddTransient<Papst.EventStore.Aggregation.IEventAggregator<MyCode.FooEntity, MyCode.TestEventFoo>, MyCode.TestEventFooAgg");
    source.ShouldContain("services.AddTransient<Papst.EventStore.Aggregation.IEventAggregator<MyCode.FooEntity, MyCode.TestEventFoo2>, MyCode.TestEventFooAgg");
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

    diagnostics.ShouldBeEmpty();
    outputCompilation.SyntaxTrees.Count().ShouldBe(3);

    var runResult = driver.GetRunResult();

    runResult.GeneratedTrees.Length.ShouldBe(2);
    runResult.Diagnostics.ShouldBeEmpty();

    var source = runResult.Results[0].GeneratedSources[0].SourceText.ToString();
    source.ShouldContain("services.AddTransient<Papst.EventStore.Aggregation.IEventAggregator<MyCode.FooEntity, MyCode.TestEventFoo>, MyCode.TestEventFooAgg");
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

    diagnostics.Length.ShouldBe(1);
    diagnostics.First().Id.ShouldBe("EVTSRC0002");
  }
}
