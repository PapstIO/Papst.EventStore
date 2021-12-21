using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Papst.EventStore.CodeGeneration
{

  [Generator]
  public class EventRegistrationCodeGenerator : ISourceGenerator
  {
    public void Execute(GeneratorExecutionContext context)
    {
      var entryPoint = context.Compilation.GetEntryPoint(context.CancellationToken);
      if (entryPoint == null)
      {
        throw new ArgumentNullException(nameof(entryPoint));
      }
      // based on https://andrewlock.net/using-source-generators-with-a-custom-attribute--to-generate-a-nav-component-in-a-blazor-app/
      var allNodes = context.Compilation.SyntaxTrees.SelectMany(s => s.GetRoot().DescendantNodes());
      var allClasses = allNodes.Where(d => d.IsKind(SyntaxKind.ClassDeclaration)).OfType<ClassDeclarationSyntax>();

      var events = allClasses
        .Select(c => FindEvents(context.Compilation, c))
        .Where(c => c != null)
        .ToList();

      var className = "EventStoreEventAggregator";

      StringBuilder builder = new StringBuilder();
      builder.Append("");
      builder
        .AppendLine("using Microsoft.Extensions.DependencyInjection;")
        .AppendLine($"namespace {entryPoint.ContainingNamespace.ToDisplayString()};")
        .AppendLine($"public static class {className}")
        .AppendLine("{")
        .AppendLine(" public static IServiceCollection AddCodeGeneratedEvents(this IServiceCollection services)")
        .AppendLine(" {")
        .AppendLine("   var registration = new Papst.EventStore.Abstractions.EventRegistration.EventRegistration();")
        ;

      foreach (var evt in events)
      {
        builder.AppendLine($"   registration.AddEvent<{evt.Value.NameSpace}.{evt.Value.Name}>({string.Join(", ", evt.Value.Attributes.Select(attr => $"new Papst.EventStore.Abstractions.EventRegistration.EventAttributeDescriptor(\"{attr.Name}\", {(attr.IsWrite ? bool.TrueString.ToLower() : bool.FalseString.ToLower())})"))});");
      }
      builder
        .AppendLine("   return services.AddSingleton<Papst.EventStore.Abstractions.EventRegistration.IEventRegistration>(registration);")
        .AppendLine("  }")
        .AppendLine("}");

      context.AddSource("EventRegistration.g.cs", builder.ToString());
    }

    private static (List<(string Name, bool IsWrite)> Attributes, string Name, string NameSpace)? FindEvents(Compilation compilation, ClassDeclarationSyntax classDeclaration)
    {
      var attributes = classDeclaration.AttributeLists
        .SelectMany(x => x.Attributes)
        .Where(attr => attr.Name.ToString() == "EventNameAttribute" || attr.Name.ToString() == "EventName")
        .ToList();

      if (attributes.Count == 0)
      {
        return null;
      }

      var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
      var className = classDeclaration.Identifier.ValueText;
      string nsName;
      if (classDeclaration.Parent is FileScopedNamespaceDeclarationSyntax fsNsDecl)
      {
        nsName = ((IdentifierNameSyntax)fsNsDecl.Name).Identifier.ValueText;
      }
      else if (classDeclaration.Parent is NamespaceDeclarationSyntax nsDecl)
      {
        nsName = ((IdentifierNameSyntax)nsDecl.Name).Identifier.ValueText;
      }
      else
      {
        throw new InvalidOperationException($"Unable to find Namespace for Event Class {className}");
      }

      List<(string Name, bool IsWrite)> setAttributes = new List<(string Name, bool IsWrite)>();
      foreach (var attr in attributes)
      {
        string name = null;
        bool isWrite = true;
        var expr = semanticModel.GetConstantValue(attr.ArgumentList.Arguments[0].Expression).Value;

        if (expr is string name2)
        {
          name = name2;
        }
        else if (expr is bool isWrite2)
        {
          isWrite = isWrite2;
        }

        if (attr.ArgumentList.Arguments.Count > 1)
        {
          expr = semanticModel.GetConstantValue(attr.ArgumentList.Arguments[1].Expression).Value;
          if (expr is string name3)
          {
            name = name3;
          }
          else if (expr is bool isWrite2)
          {
            isWrite = isWrite2;
          }
        }
        if (name != null)
        {
          setAttributes.Add((name, isWrite));
        }
      }

      if (setAttributes.Count > 0)
      {
        return (setAttributes, className, nsName);
      }
      return null;
    }

    public void Initialize(GeneratorInitializationContext context)
    {
      // none
    }
  }
}
