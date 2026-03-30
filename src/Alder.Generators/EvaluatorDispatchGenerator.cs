using System.Collections.Immutable;
using System.Linq;
using Alder.Generators.Emitters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Alder.Generators;

[Generator]
public sealed class EvaluatorDispatchGenerator : IIncrementalGenerator
{
    private const string AttributeMetadataName = "Alder.Interpretation.EvaluatesNodeAttribute";
    private const string EvaluationContextMetadataName = "Alder.Interpretation.EvaluationContext";
    private const string BoundExprMetadataName = "Alder.Binding.BoundExpr";

    private static readonly DiagnosticDescriptor NotStaticRule = new DiagnosticDescriptor(
        "ALDR9003",
        "Evaluator class must be static",
        "Evaluator class '{0}' has [EvaluatesNode] but is not static",
        "Alder.Interpretation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingEvaluateMethodRule = new DiagnosticDescriptor(
        "ALDR9004",
        "Evaluator class missing valid Evaluate method",
        "Evaluator class '{0}' has [EvaluatesNode] but no valid public static object? Evaluate(TBoundExpr, EvaluationContext) method",
        "Alder.Interpretation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var entries = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeMetadataName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => ExtractEntry(ctx))
            .Where(static e => e != null);

        var collected = entries.Collect();

        context.RegisterSourceOutput(collected, static (spc, entries) => Emit(spc, entries));
    }

    private static EvaluatorEntry? ExtractEntry(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol evaluatorClass)
            return null;

        var attr = ctx.Attributes.FirstOrDefault();
        if (attr == null || attr.ConstructorArguments.Length != 1)
            return null;

        var kindValue = attr.ConstructorArguments[0];
        if (kindValue.Value is not int)
            return null;

        var kindField = FindEnumFieldName(kindValue);
        if (kindField == null)
            return null;

        if (!evaluatorClass.IsStatic)
            return new EvaluatorEntry(null!, null!, null!, NotStaticRule, evaluatorClass.Name);

        var compilation = ctx.SemanticModel.Compilation;
        var evaluationContextSymbol = compilation.GetTypeByMetadataName(EvaluationContextMetadataName);
        var boundExprSymbol = compilation.GetTypeByMetadataName(BoundExprMetadataName);

        if (evaluationContextSymbol == null || boundExprSymbol == null)
            return null;

        var boundExprType = FindEvaluateMethodBoundExprType(evaluatorClass, evaluationContextSymbol, boundExprSymbol);
        if (boundExprType == null)
            return new EvaluatorEntry(null!, null!, null!, MissingEvaluateMethodRule, evaluatorClass.Name);

        var evaluatorTypeName = evaluatorClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var boundExprTypeName = boundExprType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return new EvaluatorEntry(kindField, evaluatorTypeName, boundExprTypeName, null, null);
    }

    private static string? FindEnumFieldName(TypedConstant constant)
    {
        if (constant.Type == null) return null;
        var value = constant.Value;
        foreach (var member in constant.Type.GetMembers())
        {
            if (member is IFieldSymbol field && field.HasConstantValue && Equals(field.ConstantValue, value))
                return field.Name;
        }
        return null;
    }

    private static INamedTypeSymbol? FindEvaluateMethodBoundExprType(
        INamedTypeSymbol evaluatorClass,
        INamedTypeSymbol evaluationContextSymbol,
        INamedTypeSymbol boundExprSymbol)
    {
        foreach (var member in evaluatorClass.GetMembers("Evaluate"))
        {
            if (member is not IMethodSymbol method)
                continue;
            if (!method.IsStatic || method.DeclaredAccessibility != Accessibility.Public)
                continue;
            if (method.Parameters.Length != 2)
                continue;
            if (!SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, evaluationContextSymbol))
                continue;
            if (method.ReturnsVoid)
                continue;

            var paramType = method.Parameters[0].Type;
            if (paramType is INamedTypeSymbol namedParam && DerivesFrom(namedParam, boundExprSymbol))
                return namedParam;
        }
        return null;
    }

    private static bool DerivesFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
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

    private static void Emit(SourceProductionContext spc, ImmutableArray<EvaluatorEntry?> entries)
    {
        if (entries.IsDefaultOrEmpty)
            return;

        var valid = new System.Collections.Generic.List<EvaluatorEntry>();
        foreach (var entry in entries)
        {
            if (entry == null) continue;
            if (entry.ErrorRule != null)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    entry.ErrorRule,
                    Location.None,
                    entry.DiagArg));
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
        w.AppendLine("using System.Runtime.CompilerServices;");
        w.AppendLine("using Alder.Binding;");
        w.AppendLine();

        using (w.Block("namespace Alder.Interpretation"))
        {
            using (w.Block("internal sealed partial class EvaluationContext"))
            {
                w.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                w.AppendLine("private object? Dispatch(BoundExpr expr) => expr.Kind switch");
                w.AppendLine("{");
                w.Indent();
                foreach (var entry in valid)
                {
                    w.AppendLine($"BoundNodeKind.{entry.KindFieldName} => {entry.EvaluatorTypeName}.Evaluate(({entry.BoundExprTypeName})expr, this),");
                }
                w.AppendLine("_ => throw new Binding.BindingNotSupportedException(");
                w.AppendLine("    $\"Bound execution for node '{{expr.GetType().Name}}' is not implemented\")");
                w.Outdent();
                w.AppendLine("};");
            }
        }

        spc.AddSource("EvaluationContext.Dispatch.g.cs", w.ToString());
    }

    private sealed class EvaluatorEntry
    {
        public string KindFieldName { get; }
        public string EvaluatorTypeName { get; }
        public string BoundExprTypeName { get; }
        public DiagnosticDescriptor? ErrorRule { get; }
        public string? DiagArg { get; }

        public EvaluatorEntry(string kindFieldName, string evaluatorTypeName, string boundExprTypeName,
            DiagnosticDescriptor? errorRule, string? diagArg)
        {
            KindFieldName = kindFieldName;
            EvaluatorTypeName = evaluatorTypeName;
            BoundExprTypeName = boundExprTypeName;
            ErrorRule = errorRule;
            DiagArg = diagArg;
        }
    }
}
