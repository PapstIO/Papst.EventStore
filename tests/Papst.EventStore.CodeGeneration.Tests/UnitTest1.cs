using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System;
using System.Collections.Immutable;
using System.Reflection;
using Xunit;

namespace Papst.EventStore.CodeGeneration.Tests;

public class UnitTest1
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

public static class CSharpSourceGeneratorVerifier<TSourceGenerator>
    where TSourceGenerator : ISourceGenerator, new()
{
  public class Test : CSharpSourceGeneratorTest<TSourceGenerator, XUnitVerifier>
  {
    public Test()
    {
    }

    protected override CompilationOptions CreateCompilationOptions()
    {
      var compilationOptions = base.CreateCompilationOptions();
      return compilationOptions.WithSpecificDiagnosticOptions(
           compilationOptions.SpecificDiagnosticOptions.SetItems(GetNullableWarningsFromCompiler()));
    }

    public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Default;

    private static ImmutableDictionary<string, ReportDiagnostic> GetNullableWarningsFromCompiler()
    {
      string[] args = { "/warnaserror:nullable" };
      var commandLineArguments = CSharpCommandLineParser.Default.Parse(args, baseDirectory: Environment.CurrentDirectory, sdkDirectory: Environment.CurrentDirectory);
      var nullableWarnings = commandLineArguments.CompilationOptions.SpecificDiagnosticOptions;

      return nullableWarnings;
    }

    protected override ParseOptions CreateParseOptions()
    {
      return ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
    }
  }
}


