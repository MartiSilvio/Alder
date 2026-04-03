using System.Collections.Immutable;
using System.Linq;
using Alder.Generators.Emitters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Alder.Generators;

[Generator]
public sealed class BinderDispatchGenerator : IIncrementalGenerator
{
    private const string AttributeMetadataName = "Alder.Binding.BindsNodeAttribute";
    private const string BoundExprMetadataName = "Alder.Binding.BoundExpr";
    private const string BindingContextMetadataName = "Alder.Binding.BindingContext";
    private const string BinderContextMetadataName = "Alder.Binding.BinderContext";
    private const string ExprMetadataName = "Alder.Parsing.Expr";

    private static readonly DiagnosticDescriptor NotStaticRule = new DiagnosticDescriptor(
        "ALDR9001",
        "Binder class must be static",
        "Binder class '{0}' has [BindsNode] but is not static",
        "Alder.Binding",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingBindMethodRule = new DiagnosticDescriptor(
        "ALDR9002",
        "Binder class missing valid Bind method",
        "Binder class '{0}' has [BindsNode(typeof({1}))] but no valid public static BoundExpr Bind({1}, BindingContext, BinderContext) method",
        "Alder.Binding",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var binderEntries = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeMetadataName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => ExtractEntry(ctx))
            .Where(static e => e != null);

        var collected = binderEntries.Collect();

        context.RegisterSourceOutput(collected, static (spc, entries) => Emit(spc, entries));
    }

    private static BinderEntry? ExtractEntry(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol binderClass)
            return null;

        var attr = ctx.Attributes.FirstOrDefault();
        if (attr == null || attr.ConstructorArguments.Length != 1)
            return null;

        if (attr.ConstructorArguments[0].Value is not INamedTypeSymbol exprType)
            return null;

        if (!binderClass.IsStatic)
            return new BinderEntry(null!, null!, 0, NotStaticRule, binderClass.Name, exprType.Name);

        var compilation = ctx.SemanticModel.Compilation;
        var boundExprSymbol = compilation.GetTypeByMetadataName(BoundExprMetadataName);
        var bindingContextSymbol = compilation.GetTypeByMetadataName(BindingContextMetadataName);
        var binderContextSymbol = compilation.GetTypeByMetadataName(BinderContextMetadataName);

        if (boundExprSymbol == null || bindingContextSymbol == null || binderContextSymbol == null)
            return null;

        if (!HasValidBindMethod(binderClass, exprType, boundExprSymbol, bindingContextSymbol, binderContextSymbol))
            return new BinderEntry(null!, null!, 0, MissingBindMethodRule, binderClass.Name, exprType.Name);

        var depth = GetInheritanceDepth(exprType);
        var exprTypeName = exprType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var binderTypeName = binderClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return new BinderEntry(exprTypeName, binderTypeName, depth, null, null, null);
    }

    private static bool HasValidBindMethod(
        INamedTypeSymbol binderClass,
        INamedTypeSymbol exprType,
        INamedTypeSymbol boundExprSymbol,
        INamedTypeSymbol bindingContextSymbol,
        INamedTypeSymbol binderContextSymbol)
    {
        foreach (var member in binderClass.GetMembers("Bind"))
        {
            if (member is not IMethodSymbol method)
                continue;
            if (!method.IsStatic || method.DeclaredAccessibility != Accessibility.Public)
                continue;
            if (method.Parameters.Length != 3)
                continue;
            if (!SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, exprType))
                continue;
            if (!SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, bindingContextSymbol))
                continue;
            if (!SymbolEqualityComparer.Default.Equals(method.Parameters[2].Type, binderContextSymbol))
                continue;
            if (!SymbolEqualityComparer.Default.Equals(method.ReturnType, boundExprSymbol))
                continue;
            return true;
        }

        return false;
    }

    private static int GetInheritanceDepth(INamedTypeSymbol type)
    {
        var depth = 0;
        var current = type.BaseType;
        while (current != null)
        {
            depth++;
            current = current.BaseType;
        }
        return depth;
    }

    private static void Emit(SourceProductionContext spc, ImmutableArray<BinderEntry?> entries)
    {
        if (entries.IsDefaultOrEmpty)
            return;

        var valid = new System.Collections.Generic.List<BinderEntry>();
        foreach (var entry in entries)
        {
            if (entry == null) continue;
            if (entry.ErrorRule != null)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    entry.ErrorRule,
                    Location.None,
                    entry.DiagArg0,
                    entry.DiagArg1));
                continue;
            }
            valid.Add(entry);
        }

        if (valid.Count == 0)
            return;

        valid.Sort((a, b) =>
        {
            var depthCmp = b.InheritanceDepth.CompareTo(a.InheritanceDepth);
            return depthCmp != 0 ? depthCmp : string.CompareOrdinal(a.ExprTypeName, b.ExprTypeName);
        });

        var w = new SourceWriter();
        w.AppendLine("// <auto-generated/>");
        w.AppendLine("#nullable enable");
        w.AppendLine();
        w.AppendLine("using System.Runtime.CompilerServices;");
        w.AppendLine();

        using (w.Block("namespace Alder.Binding"))
        {
            using (w.Block("internal sealed partial class Binder"))
            {
                w.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                w.AppendLine("private BoundExpr Dispatch(Parsing.Expr expr, BindingContext context, BinderContext binderCtx) => expr switch");
                w.AppendLine("{");
                w.Indent();
                foreach (var entry in valid)
                {
                    w.AppendLine($"{entry.ExprTypeName} e => {entry.BinderTypeName}.Bind(e, context, binderCtx),");
                }
                w.AppendLine("_ => throw new BindingNotSupportedException(");
                w.AppendLine("    $\"Binding for expression type '{{expr.GetType().Name}}' is not implemented\")");
                w.Outdent();
                w.AppendLine("};");
            }
        }

        spc.AddSource("Binder.Dispatch.g.cs", w.ToString());
    }

    private sealed class BinderEntry
    {
        public string ExprTypeName { get; }
        public string BinderTypeName { get; }
        public int InheritanceDepth { get; }
        public DiagnosticDescriptor? ErrorRule { get; }
        public string? DiagArg0 { get; }
        public string? DiagArg1 { get; }

        public BinderEntry(string exprTypeName, string binderTypeName, int inheritanceDepth,
            DiagnosticDescriptor? errorRule, string? diagArg0, string? diagArg1)
        {
            ExprTypeName = exprTypeName;
            BinderTypeName = binderTypeName;
            InheritanceDepth = inheritanceDepth;
            ErrorRule = errorRule;
            DiagArg0 = diagArg0;
            DiagArg1 = diagArg1;
        }
    }
}
