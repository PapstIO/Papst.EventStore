using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using Xunit;

namespace Papst.EventStore.CodeGeneration.Tests;

public class CodeGeneratorTests
{
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
  //[EventSourcingEvent(Name = ""FooOld"")]
  [EventSourcingEvent(Name = ""Foo"", IsWriteName = true)]
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
    source.Should().Contain("registration.AddEvent<MyCode.TestEventFoo>(new EventAttributeDescriptor(\"Foo\", true));");

  }

  private Compilation CreateCompilation(string source)
  {
    return CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source) },
                new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.ConsoleApplication));
  }

  //[Fact]
  //public async Task TestCodeGeneration2()
  //{
  //  var code = "";
  //  var generatored = "";
  //  await new VerifyCS.Test
  //  {
  //    TestState =
  //  }

  //}
}
