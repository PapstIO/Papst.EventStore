using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using Xunit;

namespace Papst.EventStore.CodeGeneration.Tests;

public class CodeGeneratorTests
{
  [Fact]
  public void TestFindsNamespaceWithEntryPoint()
  {
    var generator = new EventRegistrationCodeGenerator();
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
    var generator = new EventRegistrationCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
    internal class Foo {}
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
  public void TestCodeGeneration()
  {
    var generator = new EventRegistrationCodeGenerator();
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
  [EventName(Name = ""Foo"", IsWriteName = true)]
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
    source.Should().Contain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.Abstractions.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");
  }

  [Fact]
  public void TestCodeGenerationFullAttributeName()
  {
    var generator = new EventRegistrationCodeGenerator();
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
    source.Should().Contain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.Abstractions.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");

  }

  [Fact]
  public void TestFileScopedNamespaceEventRegistration()
  {
    var generator = new EventRegistrationCodeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

    Compilation inputCompilation = CreateCompilation(@"
namespace MyCode;
public class Program
{
  public static void Main(string[] args)
  {
  }
}
[EventName(Name = ""Foo"", IsWriteName = true)]
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
    source.Should().Contain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.Abstractions.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");

  }

  [Fact]
  public void TestEventWithTwoDescriptors()
  {
    var generator = new EventRegistrationCodeGenerator();
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
  
  [EventName(Name = ""FooOld"")]
  [EventName(Name = ""Foo"", IsWriteName = true)]
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
    source.Should().Contain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.Abstractions.EventRegistration.EventAttributeDescriptor(\"FooOld\", true), new Papst.EventStore.Abstractions.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");

  }

  [Fact]
  public void TestEventWithReadOnlyDescriptor()
  {
    var generator = new EventRegistrationCodeGenerator();
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
  
  [EventName(Name = ""FooOld"", IsWriteName = false)]
  [EventName(Name = ""Foo"", IsWriteName = true)]
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
    source.Should().Contain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.Abstractions.EventRegistration.EventAttributeDescriptor(\"FooOld\", false), new Papst.EventStore.Abstractions.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");

  }

  [Fact]
  public void TestRecordCodeGeneration()
  {
    var generator = new EventRegistrationCodeGenerator();
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
  [EventName(Name = ""Foo"", IsWriteName = true)]
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
    source.Should().Contain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.Abstractions.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");

  }

  [Fact]
  public void TestAddsEventAggregatorToSource()
  {
    var generator = new EventRegistrationCodeGenerator();
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
  [EventName(Name = ""Foo"", IsWriteName = true)]
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
    source.Should().Contain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.Abstractions.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");
    source.Should().Contain("services.AddTransient<Papst.EventStore.Abstractions.IEventAggregator<MyCode.FooEntity, MyCode.TestEventFoo>, MyCode.TestEventFooAgg");
  }

  [Fact]
  public void TestAddsEventWithMultipleImplementationsAggregatorToSource()
  {
    var generator = new EventRegistrationCodeGenerator();
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
  [EventName(Name = ""Foo"", IsWriteName = true)]
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
    source.Should().Contain("registration.AddEvent<MyCode.TestEventFoo>(new Papst.EventStore.Abstractions.EventRegistration.EventAttributeDescriptor(\"Foo\", true));");
    source.Should().Contain("services.AddTransient<Papst.EventStore.Abstractions.IEventAggregator<MyCode.FooEntity, MyCode.TestEventFoo>, MyCode.TestEventFooAgg");
    source.Should().Contain("services.AddTransient<Papst.EventStore.Abstractions.IEventAggregator<MyCode.FooEntity, MyCode.TestEventFoo2>, MyCode.TestEventFooAgg");
  }

  [Fact]
  public void TestAddsInternalAggregatorsToRegistration()
  {
    var generator = new EventRegistrationCodeGenerator();
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
  [EventName(Name = ""Foo"", IsWriteName = true)]
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
    source.Should().Contain("services.AddTransient<Papst.EventStore.Abstractions.IEventAggregator<MyCode.FooEntity, MyCode.TestEventFoo>, MyCode.TestEventFooAgg");
  }

  private Compilation CreateCompilation(string source)
  {
    return CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source) },
                new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.ConsoleApplication));
  }

}
