using System.Collections.Generic;
using System.Collections.Immutable;
using Alder.Generators.Emitters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Alder.Generators;

[Generator]
public sealed class EmitterDispatchGenerator : IIncrementalGenerator
{
    private const string AttributeMetadataName = "Alder.Compilation.EmitsNodeAttribute";
    private const string EmissionContextMetadataName = "Alder.Compiled.Compilation.Emission.EmissionContext";
    private const string BoundExprMetadataName = "Alder.Binding.BoundExpr";
    private const string ExpressionMetadataName = "System.Linq.Expressions.Expression";

    private static readonly DiagnosticDescriptor NotStaticRule = new DiagnosticDescriptor(
        "ALDR9005",
        "Emitter class must be static",
        "Emitter class '{0}' has [EmitsNode] but is not static",
        "Alder.Compilation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingEmitMethodRule = new DiagnosticDescriptor(
        "ALDR9006",
        "Emitter class missing valid Emit method",
        "Emitter class '{0}' has [EmitsNode(BoundNodeKind.{1})] but no valid public static Expression Emit(TBoundExpr, EmissionContext) method",
        "Alder.Compilation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var entries = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeMetadataName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => ExtractEntries(ctx))
            .Where(static e => e != null)
            .SelectMany(static (entries, _) => entries!);

        var collected = entries.Collect();

        context.RegisterSourceOutput(collected, static (spc, entries) => Emit(spc, entries));
    }

    private static ImmutableArray<EmitterEntry> ExtractEntries(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol emitterClass)
            return ImmutableArray<EmitterEntry>.Empty;

        var results = ImmutableArray.CreateBuilder<EmitterEntry>();

        foreach (var attr in ctx.Attributes)
        {
            if (attr.ConstructorArguments.Length < 1)
                continue;

            var kindValue = attr.ConstructorArguments[0];
            if (kindValue.Value is not int)
                continue;

            var kindField = FindEnumFieldName(kindValue);
            if (kindField == null)
                continue;

            if (!emitterClass.IsStatic)
            {
                results.Add(new EmitterEntry(null!, null!, null!, NotStaticRule, emitterClass.Name, null));
                continue;
            }

            var compilation = ctx.SemanticModel.Compilation;
            var emissionContextSymbol = compilation.GetTypeByMetadataName(EmissionContextMetadataName);
            var boundExprSymbol = compilation.GetTypeByMetadataName(BoundExprMetadataName);
            var expressionSymbol = compilation.GetTypeByMetadataName(ExpressionMetadataName);

            if (emissionContextSymbol == null || boundExprSymbol == null || expressionSymbol == null)
                continue;

            INamedTypeSymbol? boundExprType = null;

            if (attr.ConstructorArguments.Length >= 2 &&
                attr.ConstructorArguments[1].Value is INamedTypeSymbol explicitType)
            {
                boundExprType = explicitType;
            }
            else
            {
                boundExprType = FindEmitMethodBoundExprType(
                    emitterClass, emissionContextSymbol, boundExprSymbol, expressionSymbol);
            }

            if (boundExprType == null)
            {
                results.Add(new EmitterEntry(null!, null!, null!, MissingEmitMethodRule, emitterClass.Name, kindField));
                continue;
            }

            var emitterTypeName = emitterClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var boundExprTypeName = boundExprType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            results.Add(new EmitterEntry(kindField, emitterTypeName, boundExprTypeName, null, null, null));
        }

        return results.ToImmutable();
    }

    private static string? FindEnumFieldName(TypedConstant constant)
    {
        if (constant.Type == null) return null;
        var value = constant.Value;
        foreach (var member in constant.Type.GetMembers())
        {
            if (member is IFieldSymbol { HasConstantValue: true } field && Equals(field.ConstantValue, value))
                return field.Name;
        }
        return null;
    }

    private static bool DerivesFrom(ITypeSymbol type, ITypeSymbol baseType)
    {
        var current = type;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static INamedTypeSymbol? FindEmitMethodBoundExprType(
        INamedTypeSymbol emitterClass,
        INamedTypeSymbol emissionContextSymbol,
        INamedTypeSymbol boundExprSymbol,
        INamedTypeSymbol expressionSymbol)
    {
        foreach (var member in emitterClass.GetMembers("Emit"))
        {
            if (member is not IMethodSymbol method)
                continue;
            if (!method.IsStatic || method.DeclaredAccessibility != Accessibility.Public)
                continue;
            if (method.Parameters.Length != 2)
                continue;
            if (!SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, emissionContextSymbol))
                continue;
            if (!DerivesFrom(method.ReturnType, expressionSymbol))
                continue;

            var paramType = method.Parameters[0].Type;
            if (paramType is INamedTypeSymbol namedParam && DerivesFrom(namedParam, boundExprSymbol))
                return namedParam;
        }
        return null;
    }

    private static void Emit(SourceProductionContext spc, ImmutableArray<EmitterEntry> entries)
    {
        if (entries.IsDefaultOrEmpty)
            return;

        var valid = new List<EmitterEntry>();
        foreach (var entry in entries)
        {
            if (entry.ErrorRule != null)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    entry.ErrorRule,
                    Location.None,
                    entry.DiagArg,
                    entry.DiagArg2 ?? ""));
                continue;
            }
            valid.Add(entry);
        }

        if (valid.Count == 0)
            return;

        valid.Sort((a, b) => string.CompareOrdinal(a.KindFieldName, b.KindFieldName));

        var w = new SourceWriter();
        w.AppendLine("// <auto-generated/>");
        w.AppendLine("#nullable enable");
        w.AppendLine();
        w.AppendLine("using System.Linq.Expressions;");
        w.AppendLine("using System.Runtime.CompilerServices;");
        w.AppendLine("using Alder.Binding;");
        w.AppendLine();

        using (w.Block("namespace Alder.Compiled.Compilation.Emission"))
        {
            using (w.Block("internal sealed partial class EmissionContext"))
            {
                w.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                w.AppendLine("private Expression Dispatch(BoundExpr expr) => expr.Kind switch");
                w.AppendLine("{");
                w.Indent();
                foreach (var entry in valid)
                {
                    w.AppendLine($"BoundNodeKind.{entry.KindFieldName} => {entry.EmitterTypeName}.Emit(({entry.BoundExprTypeName})expr, this),");
                }
                w.AppendLine("_ => throw new Binding.BindingNotSupportedException(");
                w.AppendLine("    $\"Bound compiled emission not implemented for '{{expr.GetType().Name}}'\")");
                w.Outdent();
                w.AppendLine("};");
            }
        }

        spc.AddSource("EmissionContext.Dispatch.g.cs", w.ToString());
    }

    private sealed class EmitterEntry
    {
        public string KindFieldName { get; }
        public string EmitterTypeName { get; }
        public string BoundExprTypeName { get; }
        public DiagnosticDescriptor? ErrorRule { get; }
        public string? DiagArg { get; }
        public string? DiagArg2 { get; }

        public EmitterEntry(string kindFieldName, string emitterTypeName, string boundExprTypeName,
            DiagnosticDescriptor? errorRule, string? diagArg, string? diagArg2)
        {
            KindFieldName = kindFieldName;
            EmitterTypeName = emitterTypeName;
            BoundExprTypeName = boundExprTypeName;
            ErrorRule = errorRule;
            DiagArg = diagArg;
            DiagArg2 = diagArg2;
        }
    }
}
