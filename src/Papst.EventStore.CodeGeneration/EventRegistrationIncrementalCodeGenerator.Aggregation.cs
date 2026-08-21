using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Papst.EventStore.CodeGeneration
{
  /// <summary>
  /// Attribute based aggregation: discovers Events marked with <c>[EventAggregation&lt;TEntity&gt;]</c> and
  /// emits an <c>EventAggregatorBase&lt;TEntity,TEvent&gt;</c> implementation per Event together with its DI
  /// registration. The generated classes are written to a separate <c>EventAggregators.g.cs</c> file.
  /// </summary>
  public partial class EventRegistrationIncrementalCodeGenerator
  {
    private const string EventAggregationAttributeName = "EventAggregationAttribute";
    private const string SkipWhenNullAttributeName = "SkipWhenNullAttribute";
    private const string DictionaryKeyAttributeName = "AggregationDictionaryKeyAttribute";
    private const string CollectionKeyAttributeName = "AggregationCollectionKeyAttribute";
    private const string IgnoreAttributeName = "AggregationIgnoreAttribute";
    private const string AggregationPropertyAttributeName = "AggregationPropertyAttribute";

    private const string IEnumerableOpen = "System.Collections.Generic.IEnumerable<T>";
    private const string IDictionaryOpen = "System.Collections.Generic.IDictionary<TKey, TValue>";
    private const string ICollectionOpen = "System.Collections.Generic.ICollection<T>";

    private static readonly SymbolDisplayFormat FqFormat = SymbolDisplayFormat.FullyQualifiedFormat;

    private sealed class GeneratedAggregatorInfo
    {
      public string EntityFullName { get; set; }
      public string EventFullName { get; set; }
      public string GeneratedClassName { get; set; }
      public string MethodBody { get; set; }

      /// <summary>Namespace-qualified pair key (without <c>global::</c>) used for conflict detection.</summary>
      public string PairKey { get; set; }
    }

    /// <summary>
    /// Scans all type declarations for <c>[EventAggregation&lt;TEntity&gt;]</c> attributes and builds the
    /// aggregator descriptors. Conflicts with hand written aggregators (<paramref name="manualPairs"/>) are
    /// skipped and reported as <c>EVTSRC0003</c>.
    /// </summary>
    private static List<GeneratedAggregatorInfo> BuildGeneratedAggregators(
      Compilation compilation,
      TypeDeclarationSyntax[] allClasses,
      HashSet<string> manualPairs,
      SourceProductionContext productionContext)
    {
      var result = new List<GeneratedAggregatorInfo>();
      var seenPairs = new HashSet<string>();

      foreach (var decl in allClasses)
      {
        var model = compilation.GetSemanticModel(decl.SyntaxTree);
        if (model.GetDeclaredSymbol(decl, productionContext.CancellationToken) is not INamedTypeSymbol eventSymbol)
        {
          continue;
        }

        foreach (var attr in eventSymbol.GetAttributes())
        {
          var attrClass = attr.AttributeClass;
          if (attrClass == null
              || attrClass.Name != EventAggregationAttributeName
              || !attrClass.IsGenericType
              || attrClass.TypeArguments.Length != 1)
          {
            continue;
          }

          if (attrClass.TypeArguments[0] is not INamedTypeSymbol entitySymbol)
          {
            continue;
          }

          string pairKey = $"{entitySymbol.ToDisplayString()}|{eventSymbol.ToDisplayString()}";

          if (manualPairs.Contains(pairKey))
          {
            ReportInfo(productionContext, "EVTSRC0003",
              "Generated aggregator skipped due to existing aggregator",
              $"An aggregator for Entity '{entitySymbol.Name}' and Event '{eventSymbol.Name}' already exists; the generated aggregator is skipped.");
            continue;
          }

          if (!seenPairs.Add(pairKey))
          {
            // only one generated aggregator per (Entity, Event) pair
            continue;
          }

          string propertyPath = GetNamedString(attr, "PropertyPath") ?? string.Empty;
          bool skipNullValues = GetNamedBool(attr, "SkipNullValues") ?? true;

          if (TryBuildAggregator(productionContext, eventSymbol, entitySymbol, propertyPath, skipNullValues, out var info))
          {
            info.PairKey = pairKey;
            result.Add(info);
          }
        }
      }

      return result;
    }

    private static bool TryBuildAggregator(
      SourceProductionContext ctx,
      INamedTypeSymbol eventSymbol,
      INamedTypeSymbol entitySymbol,
      string propertyPath,
      bool skipNullValues,
      out GeneratedAggregatorInfo info)
    {
      info = null;

      string entityFull = eventSafeFq(entitySymbol);
      string eventFull = eventSafeFq(eventSymbol);

      var eventProps = GetPublicReadableProperties(eventSymbol).ToList();

      // Determine key properties (dictionary / collection) declared on the Event
      IPropertySymbol dictKeyProp = eventProps.FirstOrDefault(p => HasAttribute(p, DictionaryKeyAttributeName));
      IPropertySymbol collectionKeyProp = eventProps.FirstOrDefault(p => HasAttribute(p, CollectionKeyAttributeName));
      string collectionTargetName = collectionKeyProp == null
        ? null
        : GetCtorString(collectionKeyProp, CollectionKeyAttributeName);

      var body = new StringBuilder();
      ITypeSymbol targetType;

      // --- Resolve the base object addressed by PropertyPath (instantiating intermediate null links) ---
      if (!TryResolvePath(ctx, entitySymbol, propertyPath, body, out ITypeSymbol pathType, out string pathExpr, out IPropertySymbol finalProp))
      {
        return false;
      }

      if (dictKeyProp != null)
      {
        var dictIface = FindConstructedInterface(pathType, IDictionaryOpen);
        if (dictIface == null)
        {
          ReportWarning(ctx, "EVTSRC0004", "Invalid aggregation target",
            $"PropertyPath '{propertyPath}' on Event '{eventSymbol.Name}' is marked with a dictionary key but does not resolve to IDictionary<,>.");
          return false;
        }
        ITypeSymbol keyType = dictIface.TypeArguments[0];
        targetType = dictIface.TypeArguments[1];
        EmitDictionaryNullInit(body, pathExpr, keyType, targetType);
        body.AppendLine($"    if (!{pathExpr}.TryGetValue(evt.{dictKeyProp.Name}, out var target))");
        body.AppendLine("    {");
        body.AppendLine($"      target = new {eventSafeFq(targetType)}();");
        body.AppendLine($"      {pathExpr}[evt.{dictKeyProp.Name}] = target;");
        body.AppendLine("    }");
      }
      else if (collectionKeyProp != null)
      {
        var collIface = FindConstructedInterface(pathType, ICollectionOpen);
        if (collIface == null)
        {
          ReportWarning(ctx, "EVTSRC0004", "Invalid aggregation target",
            $"PropertyPath '{propertyPath}' on Event '{eventSymbol.Name}' is marked with a collection key but does not resolve to ICollection<T>.");
          return false;
        }
        targetType = collIface.TypeArguments[0];
        EmitCollectionNullInit(body, pathExpr, targetType);
        body.AppendLine($"    var target = global::System.Linq.Enumerable.FirstOrDefault({pathExpr}, x => global::System.Collections.Generic.EqualityComparer<{eventSafeFq(collectionKeyProp.Type)}>.Default.Equals(x.{collectionTargetName}, evt.{collectionKeyProp.Name}));");
        body.AppendLine("    if (target is null)");
        body.AppendLine("    {");
        body.AppendLine($"      target = new {eventSafeFq(targetType)}();");
        // set the key property on the new item so it is found on subsequent events
        var keySetter = GetPublicSettableProperties(targetType).FirstOrDefault(p => p.Name == collectionTargetName);
        if (keySetter != null)
        {
          body.AppendLine($"      target.{collectionTargetName} = evt.{collectionKeyProp.Name};");
        }
        body.AppendLine($"      {pathExpr}.Add(target);");
        body.AppendLine("    }");
      }
      else
      {
        targetType = pathType;
        // instantiate the final object when it is a settable reference type with a parameterless constructor
        if (finalProp != null && CanInstantiate(finalProp))
        {
          body.AppendLine($"    {pathExpr} ??= new {eventSafeFq(pathType)}();");
        }
        body.AppendLine($"    var target = {pathExpr};");
      }

      // --- Emit property assignments ---
      var targetProps = GetPublicSettableProperties(targetType)
        .GroupBy(p => p.Name)
        .ToDictionary(g => g.Key, g => g.First());

      var keyPropNames = new HashSet<string>();
      if (dictKeyProp != null) keyPropNames.Add(dictKeyProp.Name);
      if (collectionKeyProp != null) keyPropNames.Add(collectionKeyProp.Name);

      bool targetIsRootEntity = SymbolEqualityComparer.Default.Equals(targetType, entitySymbol);

      foreach (var evtProp in eventProps)
      {
        if (keyPropNames.Contains(evtProp.Name))
        {
          continue;
        }
        if (HasAttribute(evtProp, IgnoreAttributeName))
        {
          // explicitly excluded from aggregation
          continue;
        }

        // resolve the target property name (may be remapped via [AggregationProperty])
        string targetName = GetCtorString(evtProp, AggregationPropertyAttributeName) ?? evtProp.Name;

        if (targetIsRootEntity && targetName == "Version")
        {
          // Version is maintained by the stream aggregator
          continue;
        }
        if (!targetProps.TryGetValue(targetName, out var targetProp))
        {
          continue;
        }

        string assignment = BuildAssignment(evtProp, targetProp, skipNullValues);
        if (assignment != null)
        {
          body.Append(assignment);
        }
      }

      info = new GeneratedAggregatorInfo
      {
        EntityFullName = entityFull,
        EventFullName = eventFull,
        GeneratedClassName = $"{eventSymbol.Name}_{entitySymbol.Name}_GeneratedAggregator",
        MethodBody = body.ToString(),
      };
      return true;
    }

    /// <summary>
    /// Resolves the <paramref name="propertyPath"/> from the entity, emitting null-instantiating navigation for
    /// the intermediate links only. The final segment is left to the caller (single object / dictionary /
    /// collection modes) so the correct concrete container type is instantiated. Returns the resolved type, the
    /// C# expression addressing the final object (the entity itself for an empty path) and the final property.
    /// </summary>
    private static bool TryResolvePath(
      SourceProductionContext ctx,
      INamedTypeSymbol entitySymbol,
      string propertyPath,
      StringBuilder body,
      out ITypeSymbol finalType,
      out string finalExpr,
      out IPropertySymbol finalProp)
    {
      finalType = entitySymbol;
      finalExpr = "entity";
      finalProp = null;

      if (string.IsNullOrWhiteSpace(propertyPath))
      {
        return true;
      }

      ITypeSymbol current = entitySymbol;
      string expr = "entity";
      var chain = new List<(IPropertySymbol prop, string expr)>();
      foreach (string rawSegment in propertyPath.Split('.'))
      {
        string segment = rawSegment.Trim();
        if (segment.Length == 0)
        {
          continue;
        }

        var prop = GetPublicReadableProperties(current).FirstOrDefault(p => p.Name == segment);
        if (prop == null)
        {
          ReportWarning(ctx, "EVTSRC0004", "Invalid aggregation target",
            $"PropertyPath segment '{segment}' was not found on type '{current.Name}'.");
          return false;
        }

        expr = $"{expr}.{prop.Name}";
        chain.Add((prop, expr));
        current = prop.Type;
      }

      if (chain.Count == 0)
      {
        return true;
      }

      // instantiate intermediate links only; the final container is instantiated by the caller
      for (int i = 0; i < chain.Count - 1; i++)
      {
        if (CanInstantiate(chain[i].prop))
        {
          body.AppendLine($"    {chain[i].expr} ??= new {eventSafeFq(chain[i].prop.Type)}();");
        }
      }

      finalType = current;
      finalExpr = expr;
      finalProp = chain[chain.Count - 1].prop;
      return true;
    }

    private static bool CanInstantiate(IPropertySymbol prop)
      => prop.SetMethod != null
         && !prop.SetMethod.IsInitOnly
         && prop.Type.IsReferenceType
         && HasParameterlessCtor(prop.Type);

    private static string BuildAssignment(IPropertySymbol evtProp, IPropertySymbol targetProp, bool skipNullValues)
    {
      var (evtUnderlying, evtNullableValue) = Unwrap(evtProp.Type);
      var (targetUnderlying, _) = Unwrap(targetProp.Type);

      // only map when the underlying types match to keep the generated code valid
      if (evtUnderlying.ToDisplayString(FqFormat) != targetUnderlying.ToDisplayString(FqFormat))
      {
        return null;
      }

      bool skip = EffectiveSkip(evtProp, skipNullValues);
      string src = evtProp.Name;      // Event property (right-hand side)
      string tgt = targetProp.Name;   // target entity property (left-hand side), possibly remapped
      bool targetIsNullableValue = targetProp.Type.IsValueType
        && targetProp.Type is INamedTypeSymbol tnt
        && tnt.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

      // non-nullable value type on the Event: never null
      if (evtProp.Type.IsValueType && !evtNullableValue)
      {
        return $"    target.{tgt} = evt.{src};\n";
      }

      // reference type on the Event
      if (evtProp.Type.IsReferenceType)
      {
        return skip
          ? $"    if (evt.{src} is not null) {{ target.{tgt} = evt.{src}; }}\n"
          : $"    target.{tgt} = evt.{src};\n";
      }

      // nullable value type (T?) on the Event
      if (targetIsNullableValue)
      {
        return skip
          ? $"    if (evt.{src}.HasValue) {{ target.{tgt} = evt.{src}; }}\n"
          : $"    target.{tgt} = evt.{src};\n";
      }

      // nullable value on Event -> non-nullable value on target
      return skip
        ? $"    if (evt.{src}.HasValue) {{ target.{tgt} = evt.{src}.Value; }}\n"
        : $"    target.{tgt} = evt.{src}.GetValueOrDefault();\n";
    }

    private static bool EffectiveSkip(IPropertySymbol evtProp, bool globalSkip)
    {
      var attr = evtProp.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == SkipWhenNullAttributeName);
      if (attr != null && attr.ConstructorArguments.Length == 1 && attr.ConstructorArguments[0].Value is bool b)
      {
        return b;
      }
      return globalSkip;
    }

    /// <summary>Builds the <c>EventAggregators.g.cs</c> file text.</summary>
    private static string BuildGeneratedAggregatorsFile(string baseNamespace, List<GeneratedAggregatorInfo> generated)
    {
      var builder = new StringBuilder();
      builder
        .AppendLine("// <auto-generated>")
        .AppendLine("//  This code was generated by Papst.EventStore.CodeGeneration")
        .AppendLine("//  See https://github.com/PapstIO/Papst.EventStore for more information")
        .AppendLine("// </auto-generated>")
        .AppendLine("#nullable enable")
        .AppendLine($"namespace {baseNamespace}")
        .AppendLine("{");

      foreach (var info in generated)
      {
        builder
          .AppendLine($"  internal sealed class {info.GeneratedClassName} : global::Papst.EventStore.Aggregation.EventAggregatorBase<{info.EntityFullName}, {info.EventFullName}>")
          .AppendLine("  {")
          .AppendLine($"    public override global::System.Threading.Tasks.ValueTask<{info.EntityFullName}?> ApplyAsync({info.EventFullName} evt, {info.EntityFullName} entity, global::Papst.EventStore.IAggregatorStreamContext ctx)")
          .AppendLine("    {")
          .Append(info.MethodBody)
          .AppendLine("    return AsTask(entity);")
          .AppendLine("    }")
          .AppendLine("  }");
      }

      builder.AppendLine("}");
      return builder.ToString();
    }

    #region symbol helpers

    private static IEnumerable<IPropertySymbol> GetPublicReadableProperties(ITypeSymbol type)
    {
      var seen = new HashSet<string>();
      for (ITypeSymbol current = type; current != null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
      {
        foreach (var prop in current.GetMembers().OfType<IPropertySymbol>())
        {
          if (prop.DeclaredAccessibility == Accessibility.Public
              && !prop.IsStatic
              && !prop.IsIndexer
              && prop.GetMethod != null
              && seen.Add(prop.Name))
          {
            yield return prop;
          }
        }
      }
    }

    private static IEnumerable<IPropertySymbol> GetPublicSettableProperties(ITypeSymbol type)
    {
      var seen = new HashSet<string>();
      for (ITypeSymbol current = type; current != null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
      {
        foreach (var prop in current.GetMembers().OfType<IPropertySymbol>())
        {
          if (prop.DeclaredAccessibility == Accessibility.Public
              && !prop.IsStatic
              && !prop.IsIndexer
              && prop.SetMethod != null
              && !prop.SetMethod.IsInitOnly
              && prop.SetMethod.DeclaredAccessibility == Accessibility.Public
              && seen.Add(prop.Name))
          {
            yield return prop;
          }
        }
      }
    }

    private static bool HasAttribute(ISymbol symbol, string attributeName)
      => symbol.GetAttributes().Any(a => a.AttributeClass?.Name == attributeName);

    private static string GetCtorString(IPropertySymbol prop, string attributeName)
    {
      var attr = prop.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == attributeName);
      if (attr != null && attr.ConstructorArguments.Length == 1)
      {
        return attr.ConstructorArguments[0].Value as string;
      }
      return null;
    }

    private static string GetNamedString(AttributeData attr, string name)
      => attr.NamedArguments.FirstOrDefault(a => a.Key == name).Value.Value as string;

    private static bool? GetNamedBool(AttributeData attr, string name)
    {
      var arg = attr.NamedArguments.FirstOrDefault(a => a.Key == name);
      if (arg.Key == name && arg.Value.Value is bool b)
      {
        return b;
      }
      return null;
    }

    private static (ITypeSymbol underlying, bool nullableValue) Unwrap(ITypeSymbol type)
    {
      if (type is INamedTypeSymbol nt && nt.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
      {
        return (nt.TypeArguments[0], true);
      }
      return (type, false);
    }

    private static INamedTypeSymbol FindConstructedInterface(ITypeSymbol type, string openGenericDisplay)
    {
      if (type is INamedTypeSymbol named && named.IsGenericType
          && named.OriginalDefinition.ToDisplayString() == openGenericDisplay)
      {
        return named;
      }
      return type.AllInterfaces.FirstOrDefault(i => i.OriginalDefinition.ToDisplayString() == openGenericDisplay);
    }

    private static bool HasParameterlessCtor(ITypeSymbol type)
      => type is INamedTypeSymbol nt
         && !nt.IsAbstract
         && nt.InstanceConstructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility != Accessibility.Private);

    private static void EmitDictionaryNullInit(StringBuilder body, string expr, ITypeSymbol keyType, ITypeSymbol valueType)
      => body.AppendLine($"    {expr} ??= new global::System.Collections.Generic.Dictionary<{eventSafeFq(keyType)}, {eventSafeFq(valueType)}>();");

    private static void EmitCollectionNullInit(StringBuilder body, string expr, ITypeSymbol elementType)
      => body.AppendLine($"    {expr} ??= new global::System.Collections.Generic.List<{eventSafeFq(elementType)}>();");

    /// <summary>Fully-qualified type name (with <c>global::</c>), nullable annotations stripped.</summary>
    private static string eventSafeFq(ITypeSymbol type)
      => type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString(FqFormat);

    private static void ReportInfo(SourceProductionContext ctx, string id, string title, string message)
      => ctx.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
        id, title, message, "EventRegistrationCodeGen", DiagnosticSeverity.Info, true), Location.None));

    private static void ReportWarning(SourceProductionContext ctx, string id, string title, string message)
      => ctx.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
        id, title, message, "EventRegistrationCodeGen", DiagnosticSeverity.Warning, true), Location.None));

    #endregion
  }
}
