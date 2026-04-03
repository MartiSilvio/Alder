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
    private const string CancellationTokenMetadataName = "System.Threading.CancellationToken";
    private const string ValueTaskMetadataName = "System.Threading.Tasks.ValueTask`1";

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
        "Evaluator class '{0}' has [EvaluatesNode] but no valid public static object? Evaluate(TBoundExpr, EvaluationContext) or EvaluateAsync method",
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
            return new EvaluatorEntry(null!, null!, null!, false, false, NotStaticRule, evaluatorClass.Name);

        var compilation = ctx.SemanticModel.Compilation;
        var evaluationContextSymbol = compilation.GetTypeByMetadataName(EvaluationContextMetadataName);
        var boundExprSymbol = compilation.GetTypeByMetadataName(BoundExprMetadataName);
        var cancellationTokenSymbol = compilation.GetTypeByMetadataName(CancellationTokenMetadataName);

        if (evaluationContextSymbol == null || boundExprSymbol == null || cancellationTokenSymbol == null)
            return null;

        var syncBoundExprType = FindMethodBoundExprType(evaluatorClass, "Evaluate", evaluationContextSymbol, boundExprSymbol, cancellationTokenSymbol, isAsync: false);
        var asyncBoundExprType = FindMethodBoundExprType(evaluatorClass, "EvaluateAsync", evaluationContextSymbol, boundExprSymbol, cancellationTokenSymbol, isAsync: true);

        if (syncBoundExprType == null && asyncBoundExprType == null)
            return new EvaluatorEntry(null!, null!, null!, false, false, MissingEvaluateMethodRule, evaluatorClass.Name);

        var primaryBoundExprType = syncBoundExprType ?? asyncBoundExprType!;

        var evaluatorTypeName = evaluatorClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var boundExprTypeName = primaryBoundExprType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return new EvaluatorEntry(kindField, evaluatorTypeName, boundExprTypeName, syncBoundExprType != null, asyncBoundExprType != null, null, null);
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

    private static INamedTypeSymbol? FindMethodBoundExprType(
        INamedTypeSymbol evaluatorClass,
        string methodName,
        INamedTypeSymbol evaluationContextSymbol,
        INamedTypeSymbol boundExprSymbol,
        INamedTypeSymbol cancellationTokenSymbol,
        bool isAsync)
    {
        foreach (var member in evaluatorClass.GetMembers(methodName))
        {
            if (member is not IMethodSymbol method)
                continue;
            if (!method.IsStatic || method.DeclaredAccessibility != Accessibility.Public)
                continue;
            if (method.Parameters.Length != 3)
                continue;
            if (!SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, evaluationContextSymbol))
                continue;
            if (!SymbolEqualityComparer.Default.Equals(method.Parameters[2].Type, cancellationTokenSymbol))
                continue;

            if (isAsync)
            {
                if (!method.IsAsync && !IsValueTaskOfObject(method.ReturnType))
                    continue;
            }
            else
            {
                if (method.ReturnsVoid)
                    continue;
            }

            var paramType = method.Parameters[0].Type;
            if (paramType is INamedTypeSymbol namedParam && DerivesFrom(namedParam, boundExprSymbol))
                return namedParam;
        }
        return null;
    }

    private static bool IsValueTaskOfObject(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named) return false;
        if (!named.IsGenericType) return false;
        return named.OriginalDefinition.ToDisplayString() == "System.Threading.Tasks.ValueTask<TResult>";
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
        w.AppendLine("using System.Threading.Tasks;");
        w.AppendLine("using Alder.Binding;");
        w.AppendLine();

        using (w.Block("namespace Alder.Interpretation"))
        {
            using (w.Block("internal sealed partial class EvaluationContext"))
            {
                // Sync dispatch
                w.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                w.AppendLine("private object? Dispatch(BoundExpr expr, System.Threading.CancellationToken ct) => expr.Kind switch");
                w.AppendLine("{");
                w.Indent();
                foreach (var entry in valid)
                {
                    if (!entry.HasSyncEvaluate) continue;
                    w.AppendLine($"BoundNodeKind.{entry.KindFieldName} => {entry.EvaluatorTypeName}.Evaluate(({entry.BoundExprTypeName})expr, this, ct),");
                }
                w.AppendLine("_ => throw new Binding.BindingNotSupportedException(");
                w.AppendLine("    $\"Bound execution for node '{{expr.GetType().Name}}' is not implemented\")");
                w.Outdent();
                w.AppendLine("};");

                w.AppendLine();

                // Async dispatch
                w.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                w.AppendLine("private ValueTask<object?> DispatchAsync(BoundExpr expr, System.Threading.CancellationToken ct) => expr.Kind switch");
                w.AppendLine("{");
                w.Indent();
                foreach (var entry in valid)
                {
                    if (entry.HasAsyncEvaluate)
                    {
                        w.AppendLine($"BoundNodeKind.{entry.KindFieldName} => {entry.EvaluatorTypeName}.EvaluateAsync(({entry.BoundExprTypeName})expr, this, ct),");
                    }
                    else if (entry.HasSyncEvaluate)
                    {
                        w.AppendLine($"BoundNodeKind.{entry.KindFieldName} => new ValueTask<object?>({entry.EvaluatorTypeName}.Evaluate(({entry.BoundExprTypeName})expr, this, ct)),");
                    }
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
        public bool HasSyncEvaluate { get; }
        public bool HasAsyncEvaluate { get; }
        public DiagnosticDescriptor? ErrorRule { get; }
        public string? DiagArg { get; }

        public EvaluatorEntry(string kindFieldName, string evaluatorTypeName, string boundExprTypeName,
            bool hasSyncEvaluate, bool hasAsyncEvaluate, DiagnosticDescriptor? errorRule, string? diagArg)
        {
            KindFieldName = kindFieldName;
            EvaluatorTypeName = evaluatorTypeName;
            BoundExprTypeName = boundExprTypeName;
            HasSyncEvaluate = hasSyncEvaluate;
            HasAsyncEvaluate = hasAsyncEvaluate;
            ErrorRule = errorRule;
            DiagArg = diagArg;
        }
    }
}
