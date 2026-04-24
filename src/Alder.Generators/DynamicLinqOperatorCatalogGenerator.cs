using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Alder.Generators.Emitters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Alder.Generators;

[Generator]
public sealed class DynamicLinqOperatorCatalogGenerator : IIncrementalGenerator
{
    private const string OperatorAttributeFullName = "Alder.Compiled.DynamicLinq.DynamicLinqOperatorAttribute";
    private const string DispatcherExtensionAttributeFullName = "Alder.Compiled.DynamicLinq.DynamicLinqDispatcherExtensionAttribute";
    private const string TypedStringExtensionAttributeFullName = "Alder.Compiled.DynamicLinq.DynamicLinqTypedStringExtensionAttribute";
    private const string ForwardingExtensionAttributeFullName = "Alder.Compiled.DynamicLinq.DynamicLinqForwardingExtensionAttribute";
    private const string LambdaForwardingExtensionAttributeFullName = "Alder.Compiled.DynamicLinq.DynamicLinqLambdaForwardingExtensionAttribute";
    private static readonly DiagnosticDescriptor InvalidMetadataToken = new(
        id: "ALDRDL001",
        title: "Invalid DynamicLinq metadata token",
        messageFormat: "{0}",
        category: "DynamicLinqGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var operators = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                OperatorAttributeFullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => ParseOperatorAttributes(ctx))
            .Where(static result => result.HasValue)
            .Select(static (result, _) => result!.Value);

        var dispatcherExtensions = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                DispatcherExtensionAttributeFullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => ParseDispatcherExtensionAttributes(ctx))
            .Where(static result => result.HasValue)
            .Select(static (result, _) => result!.Value);

        var typedStringExtensions = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                TypedStringExtensionAttributeFullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => ParseTypedStringExtensionAttributes(ctx))
            .Where(static result => result.HasValue)
            .Select(static (result, _) => result!.Value);

        var forwardingExtensions = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ForwardingExtensionAttributeFullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => ParseForwardingExtensionAttributes(ctx))
            .Where(static result => result.HasValue)
            .Select(static (result, _) => result!.Value);

        var lambdaForwardingExtensions = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                LambdaForwardingExtensionAttributeFullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => ParseLambdaForwardingExtensionAttributes(ctx))
            .Where(static result => result.HasValue)
            .Select(static (result, _) => result!.Value);

        var combined = operators.Collect()
            .Combine(dispatcherExtensions.Collect())
            .Combine(typedStringExtensions.Collect())
            .Combine(forwardingExtensions.Collect())
            .Combine(lambdaForwardingExtensions.Collect());
        context.RegisterSourceOutput(combined, static (spc, tuple) =>
        {
            var operatorItems = tuple.Left.Left.Left.Left.SelectMany(static result => result.Items).ToImmutableArray();
            var dispatcherExtensionItems = tuple.Left.Left.Left.Right.SelectMany(static result => result.Items).ToImmutableArray();
            var typedStringExtensionItems = tuple.Left.Left.Right.SelectMany(static result => result.Items).ToImmutableArray();
            var forwardingExtensionItems = tuple.Left.Right.SelectMany(static result => result.Items).ToImmutableArray();
            var lambdaForwardingExtensionItems = tuple.Right.SelectMany(static result => result.Items).ToImmutableArray();
            foreach (var diagnostic in tuple.Left.Left.Left.Left.SelectMany(static result => result.Diagnostics))
                spc.ReportDiagnostic(diagnostic);
            foreach (var diagnostic in tuple.Left.Left.Left.Right.SelectMany(static result => result.Diagnostics))
                spc.ReportDiagnostic(diagnostic);
            foreach (var diagnostic in tuple.Left.Left.Right.SelectMany(static result => result.Diagnostics))
                spc.ReportDiagnostic(diagnostic);
            foreach (var diagnostic in tuple.Left.Right.SelectMany(static result => result.Diagnostics))
                spc.ReportDiagnostic(diagnostic);
            foreach (var diagnostic in tuple.Right.SelectMany(static result => result.Diagnostics))
                spc.ReportDiagnostic(diagnostic);
            Emit(spc, operatorItems, dispatcherExtensionItems, typedStringExtensionItems, forwardingExtensionItems, lambdaForwardingExtensionItems);
        });
    }

    private static void Emit(
        SourceProductionContext context,
        ImmutableArray<OperatorDescriptor> operators,
        ImmutableArray<DispatcherExtensionDescriptor> dispatcherExtensions,
        ImmutableArray<TypedStringExtensionDescriptor> typedStringExtensions,
        ImmutableArray<ForwardingExtensionDescriptor> forwardingExtensions,
        ImmutableArray<LambdaForwardingExtensionDescriptor> lambdaForwardingExtensions)
    {
        if (operators.IsDefaultOrEmpty)
            return;

        context.AddSource("DynamicLinqOperatorCatalog.g.cs", EmitOperatorCatalog(operators));
        context.AddSource("DynamicQueryMethodCache.Catalog.g.cs", EmitMethodCacheCatalog(operators));
        if (!dispatcherExtensions.IsDefaultOrEmpty)
            context.AddSource("DynamicQueryDispatcher.Generated.g.cs", EmitGeneratedDispatcher(dispatcherExtensions));
        if (!dispatcherExtensions.IsDefaultOrEmpty || !typedStringExtensions.IsDefaultOrEmpty || !forwardingExtensions.IsDefaultOrEmpty || !lambdaForwardingExtensions.IsDefaultOrEmpty)
            context.AddSource("AlderLinqExtensions.Generated.g.cs", EmitGeneratedExtensions(dispatcherExtensions, typedStringExtensions, forwardingExtensions, lambdaForwardingExtensions));
    }

    private static string EmitOperatorCatalog(ImmutableArray<OperatorDescriptor> operators)
    {
        var writer = new SourceWriter();
        writer.AppendLine("// <auto-generated/>");
        writer.AppendLine("#nullable enable");
        writer.AppendLine();
        writer.AppendLine("namespace Alder.Compiled.DynamicLinq;");
        writer.AppendLine();
        using (writer.Block("internal static partial class DynamicLinqOperatorCatalog"))
        {
            using (writer.Block("private static readonly DynamicLinqOperatorDescriptor[] _operators =", "};"))
            {
                foreach (var descriptor in operators)
                    writer.AppendLine(BuildOperatorDescriptorInitializer(descriptor) + ",");
            }

            writer.AppendLine();
            writer.AppendLine("internal static ReadOnlySpan<DynamicLinqOperatorDescriptor> Operators => _operators;");
        }

        return writer.ToString();
    }

    private static string BuildOperatorDescriptorInitializer(OperatorDescriptor descriptor)
        => "new DynamicLinqOperatorDescriptor("
           + ToStringLiteral(descriptor.ExtensionName)
           + ", "
           + (descriptor.RequireEnumerableSource ? "true" : "false")
           + ", "
           + (descriptor.RequireQueryableSource ? "true" : "false")
           + ", "
           + (descriptor.RequireAsyncSource ? "true" : "false")
           + ", "
           + (descriptor.RequireUntypedSequenceResult ? "true" : "false")
           + ", "
           + (descriptor.RequireUntypedScalarResult ? "true" : "false")
           + ", "
           + (descriptor.DispatcherOperatorKind is null
               ? "null"
               : "DynamicQueryOperatorKind." + descriptor.DispatcherOperatorKind.Value)
           + ", DynamicLinqProbeType."
           + descriptor.DispatcherProbeType
           + ")";

    private static string EmitMethodCacheCatalog(ImmutableArray<OperatorDescriptor> operators)
    {
        var dispatcherOps = operators
            .Where(static descriptor => descriptor.DispatcherOperatorKind is not null)
            .Select(static descriptor => descriptor.DispatcherOperatorKind!.Value)
            .Distinct()
            .ToArray();

        var writer = new SourceWriter();
        writer.AppendLine("// <auto-generated/>");
        writer.AppendLine("#nullable enable");
        writer.AppendLine();
        writer.AppendLine("namespace Alder.Compiled.DynamicLinq;");
        writer.AppendLine();
        using (writer.Block("internal static partial class DynamicQueryMethodCache"))
        {
            using (writer.Block("private static string GetOperatorMethodName(DynamicQueryOperatorKind op)"))
            {
                using (writer.Block("return op switch", "};"))
                {
                    foreach (var op in dispatcherOps)
                        writer.AppendLine("DynamicQueryOperatorKind." + op + " => nameof(Enumerable." + GetOperatorMethodName(op) + "),");
                    writer.AppendLine("_ => throw new ArgumentOutOfRangeException(nameof(op))");
                }
            }

            writer.AppendLine();
            using (writer.Block("private static bool MatchesOperator(DynamicQueryProviderKind provider, DynamicQueryOperatorKind op, MethodInfo method, Type? selectorResultType)"))
            {
                writer.AppendLine("var parameters = method.GetParameters();");
                writer.AppendLine("var genericCount = method.GetGenericArguments().Length;");
                writer.AppendLine();
                using (writer.Block("return op switch", "};"))
                {
                    foreach (var op in dispatcherOps)
                        writer.AppendLine("DynamicQueryOperatorKind." + op + " => " + GetOperatorMatchExpression(op) + ",");
                    writer.AppendLine("_ => false");
                }
            }
        }

        return writer.ToString();
    }

    private static string EmitGeneratedExtensions(
        ImmutableArray<DispatcherExtensionDescriptor> dispatcherDescriptors,
        ImmutableArray<TypedStringExtensionDescriptor> typedStringDescriptors,
        ImmutableArray<ForwardingExtensionDescriptor> forwardingDescriptors,
        ImmutableArray<LambdaForwardingExtensionDescriptor> lambdaForwardingDescriptors)
    {
        var writer = new SourceWriter();
        writer.AppendLine("// <auto-generated/>");
        writer.AppendLine("#nullable enable");
        writer.AppendLine();
        writer.AppendLine("using System.Collections;");
        writer.AppendLine("using System.Linq.Expressions;");
        writer.AppendLine("using System.Threading.Tasks;");
        writer.AppendLine("using Alder.Compiled.DynamicLinq;");
        writer.AppendLine();
        writer.AppendLine("namespace Alder.Compiled;");
        writer.AppendLine();

        using (writer.Block("public static partial class AlderLinqExtensions"))
        {
            foreach (var descriptor in dispatcherDescriptors)
            {
                EmitDispatcherExtension(writer, descriptor, useEngineOverload: false);
                if (descriptor.IncludeEngineOverload)
                    EmitDispatcherExtension(writer, descriptor, useEngineOverload: true);
                if (descriptor.IncludeTypedResultOverload)
                {
                    EmitDispatcherTypedResultExtension(writer, descriptor, useEngineOverload: false);
                    if (descriptor.IncludeEngineOverload)
                        EmitDispatcherTypedResultExtension(writer, descriptor, useEngineOverload: true);
                }
            }

            foreach (var descriptor in typedStringDescriptors)
            {
                EmitTypedStringExtension(writer, descriptor, useEngineOverload: false);
                if (descriptor.IncludeEngineOverload)
                    EmitTypedStringExtension(writer, descriptor, useEngineOverload: true);
            }

            foreach (var descriptor in forwardingDescriptors)
                EmitForwardingExtension(writer, descriptor);

            foreach (var descriptor in lambdaForwardingDescriptors)
                EmitLambdaForwardingExtension(writer, descriptor);
        }

        return writer.ToString();
    }

    private static string EmitGeneratedDispatcher(ImmutableArray<DispatcherExtensionDescriptor> dispatcherDescriptors)
    {
        var writer = new SourceWriter();
        writer.AppendLine("// <auto-generated/>");
        writer.AppendLine("#nullable enable");
        writer.AppendLine();
        writer.AppendLine("using Alder.Compiled.Compilation;");
        writer.AppendLine();
        writer.AppendLine("namespace Alder.Compiled.DynamicLinq;");
        writer.AppendLine();

        using (writer.Block("internal static partial class DynamicQueryDispatcher"))
        {
            var emitted = new HashSet<string>();
            foreach (var descriptor in dispatcherDescriptors)
            {
                if (descriptor.SourceKind == SourceKinds.Async)
                    continue;

                var key = GetDispatcherFacadeKey(descriptor);
                if (!emitted.Add(key))
                    continue;

                EmitDispatcherFacade(writer, descriptor);
            }
        }

        return writer.ToString();
    }

    private static void EmitDispatcherExtension(SourceWriter writer, DispatcherExtensionDescriptor descriptor, bool useEngineOverload)
    {
        using (writer.Block(BuildDispatcherMethodSignature(descriptor, useEngineOverload)))
        {
            writer.AppendLine("return " + BuildDispatcherInvocation(descriptor, useEngineOverload) + ";");
        }
        writer.AppendLine();
    }

    private static void EmitDispatcherTypedResultExtension(SourceWriter writer, DispatcherExtensionDescriptor descriptor, bool useEngineOverload)
    {
        using (writer.Block(BuildDispatcherTypedResultMethodSignature(descriptor, useEngineOverload)))
        {
            writer.AppendLine("return " + BuildDispatcherTypedResultInvocation(descriptor, useEngineOverload) + ";");
        }
        writer.AppendLine();
    }

    private static void EmitTypedStringExtension(SourceWriter writer, TypedStringExtensionDescriptor descriptor, bool useEngineOverload)
    {
        using (writer.Block(BuildTypedStringMethodSignature(descriptor, useEngineOverload)))
        {
            EmitTypedStringBody(writer, descriptor, useEngineOverload);
        }
        writer.AppendLine();
    }

    private static void EmitForwardingExtension(SourceWriter writer, ForwardingExtensionDescriptor descriptor)
    {
        using (writer.Block(BuildForwardingMethodSignature(descriptor)))
        {
            writer.AppendLine("return " + BuildForwardingInvocation(descriptor) + ";");
        }
        writer.AppendLine();
    }

    private static void EmitLambdaForwardingExtension(SourceWriter writer, LambdaForwardingExtensionDescriptor descriptor)
    {
        using (writer.Block(BuildLambdaForwardingMethodSignature(descriptor)))
        {
            EmitLambdaForwardingBody(writer, descriptor);
        }
        writer.AppendLine();
    }

    private static void EmitDispatcherFacade(SourceWriter writer, DispatcherExtensionDescriptor descriptor)
    {
        using (writer.Block(BuildDispatcherFacadeSignature(descriptor)))
        {
            EmitDispatcherFacadeBody(writer, descriptor);
        }
        writer.AppendLine();
    }

    private static string GetDispatcherFacadeKey(DispatcherExtensionDescriptor descriptor)
        => string.Join("|",
            descriptor.DispatcherMethodName,
            descriptor.SourceType,
            descriptor.SecondarySourceType,
            descriptor.SecondExpressionParameter.Length == 0 ? 0 : 1,
            descriptor.ThirdExpressionParameter.Length == 0 ? 0 : 1,
            descriptor.GenericArity);

    private static string BuildDispatcherFacadeSignature(DispatcherExtensionDescriptor descriptor)
    {
        var parameters = new List<string> { RenderTypeShape(descriptor.SourceType, descriptor.GenericArity) + " source" };
        if (descriptor.SecondarySourceType != DispatcherTypeShape.None)
            parameters.Add(RenderTypeShape(descriptor.SecondarySourceType, descriptor.GenericArity) + " " + descriptor.SecondarySourceName);
        parameters.Add("AlderEngine engine");
        parameters.Add("string " + descriptor.FirstExpressionParameter);
        if (!string.IsNullOrEmpty(descriptor.SecondExpressionParameter))
            parameters.Add("string " + descriptor.SecondExpressionParameter);
        if (!string.IsNullOrEmpty(descriptor.ThirdExpressionParameter))
            parameters.Add("string " + descriptor.ThirdExpressionParameter);
        parameters.Add("IReadOnlyList<KeyValuePair<string, object?>>? variables");
        if (descriptor.SortDirection != SortDirection.None)
            parameters.Add("bool descending");

        return "internal static object "
               + descriptor.DispatcherMethodName
               + "<"
               + BuildGenericParameterList(descriptor.GenericArity)
               + ">("
               + string.Join(", ", parameters)
               + ")";
    }

    private static void EmitDispatcherFacadeBody(SourceWriter writer, DispatcherExtensionDescriptor descriptor)
    {
        var provider = IsQueryable(descriptor.SourceType)
            ? "DynamicQueryProviderKind.Queryable"
            : "DynamicQueryProviderKind.Enumerable";
        var sourceType = "typeof(" + GetPrimaryTypeParameter(descriptor.GenericArity) + ")";

        switch (descriptor.DispatcherMethodName)
        {
            case "Select":
                EmitReturnInvocation(writer, "ApplySelectOperator", provider, "source", "engine", descriptor.FirstExpressionParameter, "variables", sourceType);
                break;
            case "SelectMany" when string.IsNullOrEmpty(descriptor.SecondExpressionParameter):
                EmitSingleLambdaDispatcherFacade(writer, provider, "SelectMany", descriptor.FirstExpressionParameter, sourceType, "CollectionSelector");
                break;
            case "SelectMany":
                EmitReturnInvocation(writer, "ApplySelectManyWithInferredCollectionElement", provider, "source", "engine", descriptor.FirstExpressionParameter, descriptor.SecondExpressionParameter, "variables", sourceType);
                break;
            case "OrderBy":
                EmitReturnInvocation(writer, "ApplyOrderedOperator", provider, "source", "engine", descriptor.FirstExpressionParameter, "variables", "descending ? DynamicQueryOperatorKind.OrderByDescending : DynamicQueryOperatorKind.OrderBy", sourceType);
                break;
            case "ThenBy":
                EmitReturnInvocation(writer, "ApplyOrderedOperator", provider, "source", "engine", descriptor.FirstExpressionParameter, "variables", "descending ? DynamicQueryOperatorKind.ThenByDescending : DynamicQueryOperatorKind.ThenBy", sourceType);
                break;
            case "GroupBy":
                EmitSingleLambdaDispatcherFacade(writer, provider, "GroupBy", descriptor.FirstExpressionParameter, sourceType, "KeySelector");
                break;
            case "Min":
                EmitSingleLambdaDispatcherFacade(writer, provider, "Min", descriptor.FirstExpressionParameter, sourceType, "AggregateSelector");
                break;
            case "Max":
                EmitSingleLambdaDispatcherFacade(writer, provider, "Max", descriptor.FirstExpressionParameter, sourceType, "AggregateSelector");
                break;
            case "Sum":
                EmitSingleLambdaDispatcherFacade(writer, provider, "Sum", descriptor.FirstExpressionParameter, sourceType, "AggregateSelector");
                break;
            case "Average":
                EmitSingleLambdaDispatcherFacade(writer, provider, "Average", descriptor.FirstExpressionParameter, sourceType, "AggregateSelector");
                break;
            case "Join":
                EmitJoinLikeDispatcherFacade(writer, provider, "Join", descriptor);
                break;
            case "GroupJoin":
                EmitJoinLikeDispatcherFacade(writer, provider, "GroupJoin", descriptor);
                break;
            default:
                throw new InvalidOperationException("Unsupported generated dispatcher facade '" + descriptor.DispatcherMethodName + "'.");
        }
    }

    private static void EmitSingleLambdaDispatcherFacade(
        SourceWriter writer,
        string provider,
        string operatorName,
        string expressionParameter,
        string sourceType,
        string lambdaKind)
        => EmitReturnInvocation(
            writer,
            "ApplySingleLambdaOperator",
            provider,
            "DynamicQueryOperatorKind." + operatorName,
            "source",
            "engine",
            expressionParameter,
            "variables",
            sourceType,
            "DynamicQueryLambdaKind." + lambdaKind);

    private static void EmitJoinLikeDispatcherFacade(
        SourceWriter writer,
        string provider,
        string operatorName,
        DispatcherExtensionDescriptor descriptor)
        => EmitReturnInvocation(
            writer,
            "ApplyJoinLike",
            provider,
            "DynamicQueryOperatorKind." + operatorName,
            "source",
            descriptor.SecondarySourceName,
            "engine",
            descriptor.FirstExpressionParameter,
            descriptor.SecondExpressionParameter,
            descriptor.ThirdExpressionParameter,
            "variables",
            "typeof(TOuter)",
            "typeof(TInner)");

    private static void EmitReturnInvocation(SourceWriter writer, string methodName, params string[] arguments)
    {
        writer.AppendLine("return " + methodName + "(");
        writer.Indent();
        for (var i = 0; i < arguments.Length; i++)
        {
            var suffix = i == arguments.Length - 1 ? ");" : ",";
            writer.AppendLine(arguments[i] + suffix);
        }
        writer.Outdent();
    }

    private static string BuildDispatcherMethodSignature(DispatcherExtensionDescriptor descriptor, bool useEngineOverload)
    {
        var parameters = new List<string> { "this " + RenderTypeShape(descriptor.SourceType, descriptor.GenericArity) + " source" };
        if (descriptor.SecondarySourceType != DispatcherTypeShape.None)
            parameters.Add(RenderTypeShape(descriptor.SecondarySourceType, descriptor.GenericArity) + " " + descriptor.SecondarySourceName);
        if (useEngineOverload)
            parameters.Add("AlderEngine engine");
        parameters.Add("string " + descriptor.FirstExpressionParameter);
        if (!string.IsNullOrEmpty(descriptor.SecondExpressionParameter))
            parameters.Add("string " + descriptor.SecondExpressionParameter);
        if (!string.IsNullOrEmpty(descriptor.ThirdExpressionParameter))
            parameters.Add("string " + descriptor.ThirdExpressionParameter);
        parameters.Add("params object?[] variables");

        return "public static "
               + RenderDispatcherReturnType(descriptor, includeTypedResult: false)
               + " "
               + descriptor.ExtensionMethodName
               + "<"
               + BuildGenericParameterList(descriptor.GenericArity)
               + ">("
               + string.Join(", ", parameters)
               + ")";
    }

    private static string BuildDispatcherTypedResultMethodSignature(DispatcherExtensionDescriptor descriptor, bool useEngineOverload)
    {
        var parameters = new List<string> { "this " + RenderTypeShape(descriptor.SourceType, descriptor.GenericArity) + " source" };
        if (descriptor.SecondarySourceType != DispatcherTypeShape.None)
            parameters.Add(RenderTypeShape(descriptor.SecondarySourceType, descriptor.GenericArity) + " " + descriptor.SecondarySourceName);
        if (useEngineOverload)
            parameters.Add("AlderEngine engine");
        parameters.Add("string " + descriptor.FirstExpressionParameter);
        if (!string.IsNullOrEmpty(descriptor.SecondExpressionParameter))
            parameters.Add("string " + descriptor.SecondExpressionParameter);
        if (!string.IsNullOrEmpty(descriptor.ThirdExpressionParameter))
            parameters.Add("string " + descriptor.ThirdExpressionParameter);
        parameters.Add("params object?[] variables");

        return "public static "
               + RenderDispatcherReturnType(descriptor, includeTypedResult: true)
               + " "
               + descriptor.ExtensionMethodName
               + "<"
               + BuildGenericParameterList(descriptor.GenericArity, includeResultType: true)
               + ">("
               + string.Join(", ", parameters)
               + ")";
    }

    private static string BuildDispatcherInvocation(DispatcherExtensionDescriptor descriptor, bool useEngineOverload)
    {
        var engineExpr = useEngineOverload ? "ValidateEngine(engine)" : "GetGlobalEngine()";
        if (descriptor.SourceKind == SourceKinds.Async)
            return BuildAsyncDispatcherInvocation(descriptor, engineExpr);

        var args = new List<string> { "source" };
        if (descriptor.SecondarySourceType != DispatcherTypeShape.None)
            args.Add(descriptor.SecondarySourceName);
        args.Add(engineExpr);
        args.Add(descriptor.FirstExpressionParameter);
        if (!string.IsNullOrEmpty(descriptor.SecondExpressionParameter))
            args.Add(descriptor.SecondExpressionParameter);
        if (!string.IsNullOrEmpty(descriptor.ThirdExpressionParameter))
            args.Add(descriptor.ThirdExpressionParameter);
        args.Add("BuildOrderedValues(variables)");

        if (descriptor.SortDirection != SortDirection.None)
            args.Add("descending: " + (descriptor.SortDirection == SortDirection.Descending ? "true" : "false"));

        return "(" + RenderTypeShape(descriptor.ReturnType, descriptor.GenericArity) + ")DynamicQueryDispatcher." + descriptor.DispatcherMethodName + "(" + string.Join(", ", args) + ")";
    }

    private static string BuildDispatcherTypedResultInvocation(DispatcherExtensionDescriptor descriptor, bool useEngineOverload)
    {
        var invocation = BuildDispatcherInvocation(descriptor, useEngineOverload);
        return descriptor.SourceKind == SourceKinds.Async
            ? "AsyncConvertScalarCore<TResult>(" + invocation + ")"
            : "(TResult)" + invocation;
    }

    private static string BuildAsyncDispatcherInvocation(DispatcherExtensionDescriptor descriptor, string engineExpr)
    {
        var args = new List<string>
        {
            "source",
            engineExpr,
            descriptor.FirstExpressionParameter,
            "BuildOrderedValues(variables)"
        };

        return descriptor.DispatcherMethodName switch
        {
            "Select" => "AsyncSelectBoxedCore<T>(" + string.Join(", ", args) + ")",
            "SelectMany" => "AsyncSelectManyBoxedCore<T>(" + string.Join(", ", args) + ")",
            "Sum" => "AsyncSumObjectCore<T>(" + string.Join(", ", args) + ")",
            "Average" => "AsyncAverageObjectCore<T>(" + string.Join(", ", args) + ")",
            "Min" => "AsyncMinObjectCore<T>(" + string.Join(", ", args) + ")",
            "Max" => "AsyncMaxObjectCore<T>(" + string.Join(", ", args) + ")",
            _ => throw new InvalidOperationException("Unsupported async dispatcher method '" + descriptor.DispatcherMethodName + "'.")
        };
    }

    private static string BuildTypedStringMethodSignature(TypedStringExtensionDescriptor descriptor, bool useEngineOverload)
    {
        var parameters = new List<string> { "this " + RenderTypedTypeShape(descriptor.SourceType, descriptor) + " " + GetSourceParameterName(descriptor) };
        if (descriptor.SecondarySourceType != DispatcherTypeShape.None)
            parameters.Add(RenderTypedTypeShape(descriptor.SecondarySourceType, descriptor) + " " + descriptor.SecondarySourceName);
        if (useEngineOverload)
            parameters.Add("AlderEngine engine");
        parameters.Add("string " + descriptor.FirstExpressionParameter);
        if (!string.IsNullOrEmpty(descriptor.SecondExpressionParameter))
            parameters.Add("string " + descriptor.SecondExpressionParameter);
        if (!string.IsNullOrEmpty(descriptor.ThirdExpressionParameter))
            parameters.Add("string " + descriptor.ThirdExpressionParameter);
        parameters.Add("params object?[] variables");

        return "public static "
               + RenderTypedTypeShape(descriptor.ReturnType, descriptor)
               + " "
               + descriptor.ExtensionMethodName
               + "<"
               + BuildTypedGenericParameterList(descriptor)
               + ">("
               + string.Join(", ", parameters)
               + ")";
    }

    private static string BuildForwardingMethodSignature(ForwardingExtensionDescriptor descriptor)
    {
        var parameters = new List<string> { "this " + RenderForwardingTypeShape(descriptor.SourceType, descriptor) + " source" };
        if (descriptor.SecondarySourceType != DispatcherTypeShape.None)
            parameters.Add(RenderForwardingTypeShape(descriptor.SecondarySourceType, descriptor) + " " + descriptor.SecondarySourceName);
        if (descriptor.ValueParameterType != DispatcherTypeShape.None)
            parameters.Add(RenderForwardingValueParameterType(descriptor.ValueParameterType) + " " + descriptor.ValueParameterName);

        var genericParameters = string.IsNullOrWhiteSpace(descriptor.GenericParameters)
            ? ""
            : "<" + descriptor.GenericParameters + ">";

        return "public static "
               + RenderForwardingTypeShape(descriptor.ReturnType, descriptor)
               + " "
               + descriptor.ExtensionMethodName
               + genericParameters
               + "("
               + string.Join(", ", parameters)
               + ")";
    }

    private static string BuildLambdaForwardingMethodSignature(LambdaForwardingExtensionDescriptor descriptor)
    {
        var parameters = new List<string>
        {
            "this " + RenderLambdaForwardingTypeShape(descriptor.SourceType, descriptor) + " source",
            RenderLambdaParameterType(descriptor) + " " + descriptor.LambdaParameterName
        };

        var genericParameters = string.IsNullOrWhiteSpace(descriptor.GenericParameters)
            ? ""
            : "<" + descriptor.GenericParameters + ">";

        return "public static "
               + RenderLambdaForwardingTypeShape(descriptor.ReturnType, descriptor)
               + " "
               + descriptor.ExtensionMethodName
               + genericParameters
               + "("
               + string.Join(", ", parameters)
               + ")";
    }

    private static void EmitLambdaForwardingBody(SourceWriter writer, LambdaForwardingExtensionDescriptor descriptor)
    {
        if (descriptor.SourceKind == SourceKinds.Async)
        {
            EmitAsyncLambdaForwardingBody(writer, descriptor);
            return;
        }

        var genericArguments = string.IsNullOrWhiteSpace(descriptor.LinqGenericParameters)
            ? ""
            : "<" + descriptor.LinqGenericParameters + ">";
        var lambdaArg = descriptor.LambdaKind switch
        {
            LambdaForwardingKind.ExpressionPredicate
                or LambdaForwardingKind.ExpressionSelector
                or LambdaForwardingKind.ExpressionKeySelector
                or LambdaForwardingKind.ExpressionCollectionSelector
                or LambdaForwardingKind.ExpressionDecimalSelector
                when descriptor.SourceKind == SourceKinds.Enumerable => "Compile" + GetLambdaCompileHelperSuffix(descriptor.LambdaKind) + "(" + descriptor.LambdaParameterName + ")",
            _ => descriptor.LambdaParameterName
        };
        var invocation = "source." + descriptor.LinqMethodName + genericArguments + "(" + lambdaArg + ")";
        if (descriptor.SourceKind == SourceKinds.Queryable || descriptor.LambdaKind is LambdaForwardingKind.FuncPredicate or LambdaForwardingKind.FuncSelector or LambdaForwardingKind.FuncCollectionSelector)
            writer.AppendLine("ArgumentNullException.ThrowIfNull(" + descriptor.LambdaParameterName + ");");

        writer.AppendLine("return " + invocation + ";");
    }

    private static void EmitAsyncLambdaForwardingBody(SourceWriter writer, LambdaForwardingExtensionDescriptor descriptor)
    {
        switch (descriptor.LambdaKind)
        {
            case LambdaForwardingKind.ExpressionPredicate when descriptor.LinqMethodName == "Where":
                writer.AppendLine("return AsyncWhereCore(source, CompilePredicate(" + descriptor.LambdaParameterName + "));");
                break;
            case LambdaForwardingKind.ExpressionPredicate when descriptor.LinqMethodName == "Any":
                writer.AppendLine("return AsyncAnyCore(source, CompilePredicate(" + descriptor.LambdaParameterName + "));");
                break;
            case LambdaForwardingKind.ExpressionPredicate when descriptor.LinqMethodName == "Count":
                writer.AppendLine("return AsyncCountCore(source, CompilePredicate(" + descriptor.LambdaParameterName + "));");
                break;
            case LambdaForwardingKind.FuncPredicate when descriptor.LinqMethodName == "Where":
                writer.AppendLine("ArgumentNullException.ThrowIfNull(" + descriptor.LambdaParameterName + ");");
                writer.AppendLine("return AsyncWhereCore(source, " + descriptor.LambdaParameterName + ");");
                break;
            case LambdaForwardingKind.FuncPredicate when descriptor.LinqMethodName == "Any":
                writer.AppendLine("ArgumentNullException.ThrowIfNull(" + descriptor.LambdaParameterName + ");");
                writer.AppendLine("return AsyncAnyCore(source, " + descriptor.LambdaParameterName + ");");
                break;
            case LambdaForwardingKind.FuncPredicate when descriptor.LinqMethodName == "Count":
                writer.AppendLine("ArgumentNullException.ThrowIfNull(" + descriptor.LambdaParameterName + ");");
                writer.AppendLine("return AsyncCountCore(source, " + descriptor.LambdaParameterName + ");");
                break;
            case LambdaForwardingKind.ExpressionSelector when descriptor.LinqMethodName == "Select":
                writer.AppendLine("return AsyncSelectCore(source, CompileSelector(" + descriptor.LambdaParameterName + "));");
                break;
            case LambdaForwardingKind.FuncSelector when descriptor.LinqMethodName == "Select":
                writer.AppendLine("ArgumentNullException.ThrowIfNull(" + descriptor.LambdaParameterName + ");");
                writer.AppendLine("return AsyncSelectCore(source, " + descriptor.LambdaParameterName + ");");
                break;
            case LambdaForwardingKind.ExpressionCollectionSelector when descriptor.LinqMethodName == "SelectMany":
                writer.AppendLine("return AsyncSelectManyCore(source, CompileSelector(" + descriptor.LambdaParameterName + "));");
                break;
            case LambdaForwardingKind.FuncCollectionSelector when descriptor.LinqMethodName == "SelectMany":
                writer.AppendLine("ArgumentNullException.ThrowIfNull(" + descriptor.LambdaParameterName + ");");
                writer.AppendLine("return AsyncSelectManyCore(source, " + descriptor.LambdaParameterName + ");");
                break;
            case LambdaForwardingKind.ExpressionDecimalSelector when descriptor.LinqMethodName == "Sum":
                writer.AppendLine("return AsyncSumDecimalCore(source, CompileSelector(" + descriptor.LambdaParameterName + "));");
                break;
            case LambdaForwardingKind.FuncDecimalSelector when descriptor.LinqMethodName == "Sum":
                writer.AppendLine("ArgumentNullException.ThrowIfNull(" + descriptor.LambdaParameterName + ");");
                writer.AppendLine("return AsyncSumDecimalCore(source, " + descriptor.LambdaParameterName + ");");
                break;
            default:
                throw new InvalidOperationException("Unsupported async lambda forwarding kind '" + descriptor.LambdaKind + "'.");
        }
    }

    private static string BuildForwardingInvocation(ForwardingExtensionDescriptor descriptor)
    {
        if (descriptor.SourceKind == SourceKinds.Async)
        {
            return descriptor.LinqMethodName switch
            {
                "Skip" => "AsyncSkipCore(source, count)",
                "Take" => "AsyncTakeCore(source, count)",
                "Distinct" => "AsyncDistinctCore(source)",
                "Reverse" => "AsyncReverseCore(source)",
                _ => throw new InvalidOperationException("Unsupported async forwarding method '" + descriptor.LinqMethodName + "'.")
            };
        }

        var args = new List<string>();
        if (descriptor.SecondarySourceType != DispatcherTypeShape.None)
            args.Add(descriptor.SecondarySourceName);
        if (descriptor.ValueParameterType != DispatcherTypeShape.None)
            args.Add(descriptor.ValueParameterName);

        var genericArguments = string.IsNullOrWhiteSpace(descriptor.GenericParameters)
            ? ""
            : "<" + descriptor.GenericParameters + ">";
        var invocation = "source." + descriptor.LinqMethodName + genericArguments + "(" + string.Join(", ", args) + ")";
        return descriptor.NullForgivingResult ? invocation + "!" : invocation;
    }

    private static string RenderLambdaParameterType(LambdaForwardingExtensionDescriptor descriptor)
    {
        var lambdaType = descriptor.LambdaKind switch
        {
            LambdaForwardingKind.ExpressionPredicate => "Expression<Func<T, bool>>",
            LambdaForwardingKind.FuncPredicate => "Func<T, bool>",
            LambdaForwardingKind.ExpressionSelector => "Expression<Func<T, TResult>>",
            LambdaForwardingKind.FuncSelector => "Func<T, TResult>",
            LambdaForwardingKind.ExpressionKeySelector => "Expression<Func<T, TKey>>",
            LambdaForwardingKind.FuncKeySelector => "Func<T, TKey>",
            LambdaForwardingKind.ExpressionCollectionSelector => "Expression<Func<T, IEnumerable<TElement>>>",
            LambdaForwardingKind.FuncCollectionSelector => "Func<T, IEnumerable<TElement>>",
            LambdaForwardingKind.ExpressionDecimalSelector => "Expression<Func<T, decimal>>",
            LambdaForwardingKind.FuncDecimalSelector => "Func<T, decimal>",
            _ => throw new InvalidOperationException("Unsupported lambda forwarding kind '" + descriptor.LambdaKind + "'.")
        };
        return lambdaType;
    }

    private static string GetLambdaCompileHelperSuffix(LambdaForwardingKind kind) =>
        kind switch
        {
            LambdaForwardingKind.ExpressionPredicate => "Predicate",
            LambdaForwardingKind.ExpressionSelector => "Selector",
            LambdaForwardingKind.ExpressionKeySelector => "Selector",
            LambdaForwardingKind.ExpressionCollectionSelector => "Selector",
            LambdaForwardingKind.ExpressionDecimalSelector => "Selector",
            _ => throw new InvalidOperationException("Unsupported compiled lambda forwarding kind '" + kind + "'.")
        };

    private static void EmitTypedStringBody(SourceWriter writer, TypedStringExtensionDescriptor descriptor, bool useEngineOverload)
    {
        if (descriptor.SourceKind == SourceKinds.Async)
        {
            EmitAsyncTypedStringBody(writer, descriptor, useEngineOverload);
            return;
        }

        var source = GetSourceParameterName(descriptor);
        var engine = useEngineOverload ? "engine" : "null";
        var validatedEngine = useEngineOverload ? "ValidateEngine(engine)" : "GetGlobalEngine()";
        var operatorName = GetTypedLinqMethodName(descriptor);

        switch (descriptor.LambdaKind)
        {
            case TypedStringLambdaKind.Predicate:
            case TypedStringLambdaKind.Selector:
            case TypedStringLambdaKind.MaterializingSelector:
            case TypedStringLambdaKind.CollectionSelector:
                EmitUnaryTypedStringReturn(writer, descriptor, source, engine, validatedEngine, operatorName);
                break;
            case TypedStringLambdaKind.Grouping:
                EmitUnaryTypedStringReturn(writer, descriptor, source, engine, validatedEngine, "GroupBy");
                break;
            case TypedStringLambdaKind.SelectManyResultSelector:
                EmitSelectManyResultBody(writer, descriptor, source, engine, operatorName);
                break;
            case TypedStringLambdaKind.Join:
                EmitJoinBody(writer, descriptor, source, engine, groupJoin: false);
                break;
            case TypedStringLambdaKind.GroupJoin:
                EmitJoinBody(writer, descriptor, source, engine, groupJoin: true);
                break;
            default:
                throw new InvalidOperationException("Unsupported typed string lambda kind '" + descriptor.LambdaKind + "'.");
        }
    }

    private static void EmitUnaryTypedStringReturn(
        SourceWriter writer,
        TypedStringExtensionDescriptor descriptor,
        string source,
        string engine,
        string validatedEngine,
        string methodName)
    {
        var unaryFactory = GetUnaryFactory(descriptor, engine, validatedEngine, queryable: IsQueryable(descriptor.SourceType));
        writer.AppendLine("return " + source + "." + methodName + "(" + unaryFactory + ");");
    }

    private static void EmitAsyncTypedStringBody(SourceWriter writer, TypedStringExtensionDescriptor descriptor, bool useEngineOverload)
    {
        var engine = useEngineOverload ? "ValidateEngine(engine)" : "GetGlobalEngine()";
        var compiledPredicate = "CompilePredicate<T>(" + engine + ", " + descriptor.FirstExpressionParameter + ", variables)";
        switch (descriptor.LambdaKind)
        {
            case TypedStringLambdaKind.Predicate:
                writer.AppendLine("return " + GetAsyncPredicateCoreMethodName(descriptor.LinqMethodName) + "(source, " + compiledPredicate + ");");
                break;
            case TypedStringLambdaKind.MaterializingSelector:
                writer.AppendLine("return AsyncSelectCore(source, CompileMaterializingSelector<T, TResult>(" + engine + ", " + descriptor.FirstExpressionParameter + ", BuildOrderedValues(variables)));");
                break;
            case TypedStringLambdaKind.CollectionSelector:
                writer.AppendLine("return AsyncSelectManyCore(source, CompileCollectionSelector<T, TElement>(" + engine + ", " + descriptor.FirstExpressionParameter + ", variables));");
                break;
            default:
                throw new InvalidOperationException("Unsupported async typed string lambda kind '" + descriptor.LambdaKind + "'.");
        }
    }

    private static string GetAsyncPredicateCoreMethodName(string methodName) =>
        methodName switch
        {
            "Where" => "AsyncWhereCore",
            "Any" => "AsyncAnyCore",
            "All" => "AsyncAllCore",
            "Count" => "AsyncCountCore",
            "LongCount" => "AsyncLongCountCore",
            "First" => "AsyncFirstCore",
            "FirstOrDefault" => "AsyncFirstOrDefaultCore",
            "Last" => "AsyncLastCore",
            "LastOrDefault" => "AsyncLastOrDefaultCore",
            "Single" => "AsyncSingleCore",
            "SingleOrDefault" => "AsyncSingleOrDefaultCore",
            "SkipWhile" => "AsyncSkipWhileCore",
            "TakeWhile" => "AsyncTakeWhileCore",
            _ => throw new InvalidOperationException("Unsupported async predicate method '" + methodName + "'.")
        };

    private static void EmitSelectManyResultBody(
        SourceWriter writer,
        TypedStringExtensionDescriptor descriptor,
        string source,
        string engine,
        string operatorName)
    {
        if (IsQueryable(descriptor.SourceType))
        {
            EmitReturnInvocation(
                writer,
                source + "." + operatorName,
                "ParseCollectionSelector<T, TElement>(" + engine + ", " + descriptor.FirstExpressionParameter + ", variables)",
                "ParseBinaryLambda<T, TElement, TResult>(" + engine + ", " + descriptor.SecondExpressionParameter + ", variables, \"outer\", \"inner\")");
            return;
        }

        EmitReturnInvocation(
            writer,
            source + "." + operatorName,
            "CompileCollectionSelector<T, TElement>(" + engine + ", " + descriptor.FirstExpressionParameter + ", variables)",
            "CompileBinaryLambda<T, TElement, TResult>(" + engine + ", " + descriptor.SecondExpressionParameter + ", variables, \"outer\", \"inner\")");
    }

    private static void EmitJoinBody(
        SourceWriter writer,
        TypedStringExtensionDescriptor descriptor,
        string source,
        string engine,
        bool groupJoin)
    {
        var operatorName = groupJoin ? "GroupJoin" : "Join";
        var resultSelectorFactory = groupJoin
            ? (IsQueryable(descriptor.SourceType)
                ? "ParseBinaryLambda<TOuter, IEnumerable<TInner>, TResult>(" + engine + ", " + descriptor.ThirdExpressionParameter + ", variables, \"outer\", \"group\")"
                : "CompileBinaryLambda<TOuter, IEnumerable<TInner>, TResult>(" + engine + ", " + descriptor.ThirdExpressionParameter + ", variables, \"outer\", \"group\")")
            : (IsQueryable(descriptor.SourceType)
                ? "ParseBinaryLambda<TOuter, TInner, TResult>(" + engine + ", " + descriptor.ThirdExpressionParameter + ", variables, \"outer\", \"inner\")"
                : "CompileBinaryLambda<TOuter, TInner, TResult>(" + engine + ", " + descriptor.ThirdExpressionParameter + ", variables, \"outer\", \"inner\")");

        if (IsQueryable(descriptor.SourceType))
        {
            EmitReturnInvocation(
                writer,
                source + "." + operatorName,
                descriptor.SecondarySourceName + ".AsQueryable()",
                "ParseSelector<TOuter, TKey>(" + engine + ", " + descriptor.FirstExpressionParameter + ", variables)",
                "ParseSelector<TInner, TKey>(" + engine + ", " + descriptor.SecondExpressionParameter + ", variables)",
                resultSelectorFactory);
            return;
        }

        EmitReturnInvocation(
            writer,
            source + "." + operatorName,
            descriptor.SecondarySourceName,
            "CompileSelector<TOuter, TKey>(" + engine + ", " + descriptor.FirstExpressionParameter + ", variables)",
            "CompileSelector<TInner, TKey>(" + engine + ", " + descriptor.SecondExpressionParameter + ", variables)",
            resultSelectorFactory);
    }

    private static string GetUnaryFactory(
        TypedStringExtensionDescriptor descriptor,
        string engine,
        string validatedEngine,
        bool queryable)
    {
        var expression = descriptor.FirstExpressionParameter;
        return descriptor.LambdaKind switch
        {
            TypedStringLambdaKind.Predicate => queryable
                ? "ParsePredicate<T>(" + engine + ", " + expression + ", variables)"
                : "CompilePredicate<T>(" + engine + ", " + expression + ", variables)",
            TypedStringLambdaKind.Selector => queryable
                ? "ParseSelector<T, TKey>(" + engine + ", " + expression + ", variables)"
                : "CompileSelector<T, TKey>(" + engine + ", " + expression + ", variables)",
            TypedStringLambdaKind.MaterializingSelector => queryable
                ? "ParseMaterializingSelector<T, TResult>(" + validatedEngine + ", " + expression + ", BuildOrderedValues(variables))"
                : "CompileMaterializingSelector<T, TResult>(" + validatedEngine + ", " + expression + ", BuildOrderedValues(variables))",
            TypedStringLambdaKind.CollectionSelector => queryable
                ? "ParseCollectionSelector<T, TElement>(" + engine + ", " + expression + ", variables)"
                : "CompileCollectionSelector<T, TElement>(" + engine + ", " + expression + ", variables)",
            TypedStringLambdaKind.Grouping => queryable
                ? "ParseSelector<T, TKey>(" + engine + ", " + expression + ", variables)"
                : "CompileSelector<T, TKey>(" + engine + ", " + expression + ", variables)",
            _ => throw new InvalidOperationException("Unsupported unary typed string lambda kind '" + descriptor.LambdaKind + "'.")
        };
    }

    private static string GetTypedLinqMethodName(TypedStringExtensionDescriptor descriptor)
    {
        if (descriptor.SortDirection == SortDirection.Descending)
            return descriptor.LinqMethodName + "Descending";
        return descriptor.LinqMethodName;
    }

    private static string GetSourceParameterName(TypedStringExtensionDescriptor descriptor) =>
        descriptor.LambdaKind is TypedStringLambdaKind.Join or TypedStringLambdaKind.GroupJoin ? "outer" : "source";

    private static bool IsQueryable(DispatcherTypeShape shape) =>
        shape is DispatcherTypeShape.IQueryable or DispatcherTypeShape.IQueryableOfT or DispatcherTypeShape.IOrderedQueryableOfT;

    private static string RenderTypeShape(DispatcherTypeShape shape, int genericArity) =>
        shape switch
        {
            DispatcherTypeShape.None => "",
            DispatcherTypeShape.IEnumerable => "IEnumerable",
            DispatcherTypeShape.IQueryable => "IQueryable",
            DispatcherTypeShape.IEnumerableOfT => "IEnumerable<" + GetPrimaryTypeParameter(genericArity) + ">",
            DispatcherTypeShape.IQueryableOfT => "IQueryable<" + GetPrimaryTypeParameter(genericArity) + ">",
            DispatcherTypeShape.IAsyncEnumerableOfT => "IAsyncEnumerable<" + GetPrimaryTypeParameter(genericArity) + ">",
            DispatcherTypeShape.IEnumerableOfTSecond => "IEnumerable<" + GetSecondaryTypeParameter(genericArity) + ">",
            DispatcherTypeShape.IQueryableOfTSecond => "IQueryable<" + GetSecondaryTypeParameter(genericArity) + ">",
            DispatcherTypeShape.IOrderedEnumerableOfT => "IOrderedEnumerable<" + GetPrimaryTypeParameter(genericArity) + ">",
            DispatcherTypeShape.IOrderedQueryableOfT => "IOrderedQueryable<" + GetPrimaryTypeParameter(genericArity) + ">",
            DispatcherTypeShape.Object => "object",
            _ => throw new InvalidOperationException("Unsupported type shape '" + shape + "'.")
        };

    private static string RenderDispatcherReturnType(DispatcherExtensionDescriptor descriptor, bool includeTypedResult)
    {
        if (descriptor.SourceKind != SourceKinds.Async)
            return includeTypedResult ? "TResult" : RenderTypeShape(descriptor.ReturnType, descriptor.GenericArity);

        if (includeTypedResult)
            return "ValueTask<TResult>";

        return descriptor.ReturnType switch
        {
            DispatcherTypeShape.Object => "ValueTask<object>",
            DispatcherTypeShape.IEnumerable => "IAsyncEnumerable<object?>",
            _ => RenderTypeShape(descriptor.ReturnType, descriptor.GenericArity)
        };
    }

    private static string RenderTypedTypeShape(DispatcherTypeShape shape, TypedStringExtensionDescriptor descriptor) =>
        shape switch
        {
            _ when descriptor.SourceKind == SourceKinds.Async => shape switch
            {
                DispatcherTypeShape.SequenceOfT => "IAsyncEnumerable<T>",
                DispatcherTypeShape.IAsyncEnumerableOfT => "IAsyncEnumerable<T>",
                DispatcherTypeShape.SequenceOfTResult => "IAsyncEnumerable<TResult>",
                DispatcherTypeShape.SequenceOfTElement => "IAsyncEnumerable<TElement>",
                DispatcherTypeShape.Boolean => "ValueTask<bool>",
                DispatcherTypeShape.Int32 => "ValueTask<int>",
                DispatcherTypeShape.Int64 => "ValueTask<long>",
                DispatcherTypeShape.T => "ValueTask<" + GetPrimaryTypeParameter(descriptor.GenericArity) + ">",
                DispatcherTypeShape.NullableT => "ValueTask<" + GetPrimaryTypeParameter(descriptor.GenericArity) + "?>",
                _ => RenderTypeShape(shape, descriptor.GenericArity)
            },
            DispatcherTypeShape.IEnumerableOfTResult => "IEnumerable<TResult>",
            DispatcherTypeShape.IQueryableOfTResult => "IQueryable<TResult>",
            DispatcherTypeShape.IEnumerableOfTElement => "IEnumerable<TElement>",
            DispatcherTypeShape.IQueryableOfTElement => "IQueryable<TElement>",
            DispatcherTypeShape.IEnumerableOfGrouping => "IEnumerable<IGrouping<TKey, T>>",
            DispatcherTypeShape.IQueryableOfGrouping => "IQueryable<IGrouping<TKey, T>>",
            DispatcherTypeShape.Boolean => "bool",
            DispatcherTypeShape.Int32 => "int",
            DispatcherTypeShape.Int64 => "long",
            DispatcherTypeShape.T => GetPrimaryTypeParameter(descriptor.GenericArity),
            DispatcherTypeShape.NullableT => GetPrimaryTypeParameter(descriptor.GenericArity) + "?",
            _ => RenderTypeShape(shape, descriptor.GenericArity)
        };

    private static string RenderForwardingTypeShape(DispatcherTypeShape shape, ForwardingExtensionDescriptor descriptor) =>
        shape switch
        {
            _ when descriptor.SourceKind == SourceKinds.Async => shape switch
            {
                DispatcherTypeShape.SequenceOfT => "IAsyncEnumerable<T>",
                DispatcherTypeShape.IAsyncEnumerableOfT => "IAsyncEnumerable<T>",
                DispatcherTypeShape.Boolean => "ValueTask<bool>",
                DispatcherTypeShape.Int32 => "ValueTask<int>",
                DispatcherTypeShape.Int64 => "ValueTask<long>",
                DispatcherTypeShape.T => "ValueTask<T>",
                DispatcherTypeShape.NullableT => "ValueTask<T?>",
                _ => RenderTypeShape(shape, descriptor.GenericArity)
            },
            DispatcherTypeShape.Sequence => descriptor.SourceKind == SourceKinds.Queryable ? "IQueryable" : "IEnumerable",
            DispatcherTypeShape.SequenceOfT => descriptor.SourceKind == SourceKinds.Queryable ? "IQueryable<T>" : "IEnumerable<T>",
            DispatcherTypeShape.IEnumerableOfT => "IEnumerable<T>",
            DispatcherTypeShape.IQueryableOfT => "IQueryable<T>",
            DispatcherTypeShape.SequenceOfTResult => descriptor.SourceKind == SourceKinds.Queryable ? "IQueryable<TResult>" : "IEnumerable<TResult>",
            DispatcherTypeShape.IEnumerableOfTResult => "IEnumerable<TResult>",
            DispatcherTypeShape.IQueryableOfTResult => "IQueryable<TResult>",
            DispatcherTypeShape.IEnumerable => "IEnumerable",
            DispatcherTypeShape.IQueryable => "IQueryable",
            DispatcherTypeShape.Boolean => "bool",
            DispatcherTypeShape.Int32 => "int",
            DispatcherTypeShape.Int64 => "long",
            DispatcherTypeShape.T => "T",
            DispatcherTypeShape.NullableT => "T?",
            _ => RenderTypeShape(shape, descriptor.GenericArity)
        };

    private static string RenderForwardingValueParameterType(DispatcherTypeShape shape) =>
        shape switch
        {
            DispatcherTypeShape.Boolean => "bool",
            DispatcherTypeShape.Int32 => "int",
            DispatcherTypeShape.Int64 => "long",
            DispatcherTypeShape.Decimal => "decimal",
            DispatcherTypeShape.T => "T",
            DispatcherTypeShape.NullableT => "T?",
            _ => RenderTypeShape(shape, genericArity: 1)
        };

    private static string RenderLambdaForwardingTypeShape(DispatcherTypeShape shape, LambdaForwardingExtensionDescriptor descriptor) =>
        shape switch
        {
            _ when descriptor.SourceKind == SourceKinds.Async => shape switch
            {
                DispatcherTypeShape.SequenceOfT => "IAsyncEnumerable<T>",
                DispatcherTypeShape.IAsyncEnumerableOfT => "IAsyncEnumerable<T>",
                DispatcherTypeShape.SequenceOfTResult => "IAsyncEnumerable<TResult>",
                DispatcherTypeShape.SequenceOfTElement => "IAsyncEnumerable<TElement>",
                DispatcherTypeShape.Boolean => "ValueTask<bool>",
                DispatcherTypeShape.Int32 => "ValueTask<int>",
                DispatcherTypeShape.Int64 => "ValueTask<long>",
                DispatcherTypeShape.Decimal => "ValueTask<decimal>",
                _ => RenderTypeShape(shape, descriptor.GenericArity)
            },
            DispatcherTypeShape.SequenceOfT => descriptor.SourceKind == SourceKinds.Queryable ? "IQueryable<T>" : "IEnumerable<T>",
            DispatcherTypeShape.IEnumerableOfT => "IEnumerable<T>",
            DispatcherTypeShape.IQueryableOfT => "IQueryable<T>",
            DispatcherTypeShape.SequenceOfTResult => descriptor.SourceKind == SourceKinds.Queryable ? "IQueryable<TResult>" : "IEnumerable<TResult>",
            DispatcherTypeShape.IEnumerableOfTResult => "IEnumerable<TResult>",
            DispatcherTypeShape.IQueryableOfTResult => "IQueryable<TResult>",
            DispatcherTypeShape.SequenceOfTElement => descriptor.SourceKind == SourceKinds.Queryable ? "IQueryable<TElement>" : "IEnumerable<TElement>",
            DispatcherTypeShape.IEnumerableOfTElement => "IEnumerable<TElement>",
            DispatcherTypeShape.IQueryableOfTElement => "IQueryable<TElement>",
            DispatcherTypeShape.OrderedSequenceOfT => descriptor.SourceKind == SourceKinds.Queryable ? "IOrderedQueryable<T>" : "IOrderedEnumerable<T>",
            DispatcherTypeShape.IOrderedEnumerableOfT => "IOrderedEnumerable<T>",
            DispatcherTypeShape.IOrderedQueryableOfT => "IOrderedQueryable<T>",
            DispatcherTypeShape.Boolean => "bool",
            DispatcherTypeShape.Int32 => "int",
            DispatcherTypeShape.Int64 => "long",
            DispatcherTypeShape.Decimal => "decimal",
            _ => RenderTypeShape(shape, descriptor.GenericArity)
        };

    private static string GetPrimaryTypeParameter(int genericArity) =>
        genericArity switch
        {
            1 => "T",
            2 => "TOuter",
            _ => throw new InvalidOperationException("Unsupported generic arity '" + genericArity + "'.")
        };

    private static string GetSecondaryTypeParameter(int genericArity) =>
        genericArity switch
        {
            2 => "TInner",
            _ => throw new InvalidOperationException("No secondary type parameter for generic arity '" + genericArity + "'.")
        };

    private static string BuildGenericParameterList(int genericArity, bool includeResultType = false)
    {
        var genericParams = genericArity switch
        {
            1 => "T",
            2 => "TOuter, TInner",
            _ => throw new InvalidOperationException("Unsupported generic arity '" + genericArity + "'.")
        };

        return includeResultType ? genericParams + ", TResult" : genericParams;
    }

    private static string BuildTypedGenericParameterList(TypedStringExtensionDescriptor descriptor) =>
        descriptor.LambdaKind switch
        {
            TypedStringLambdaKind.Selector => BuildGenericParameterList(descriptor.GenericArity) + ", TKey",
            TypedStringLambdaKind.Grouping => BuildGenericParameterList(descriptor.GenericArity) + ", TKey",
            TypedStringLambdaKind.MaterializingSelector => BuildGenericParameterList(descriptor.GenericArity, includeResultType: true),
            TypedStringLambdaKind.CollectionSelector => BuildGenericParameterList(descriptor.GenericArity) + ", TElement",
            TypedStringLambdaKind.SelectManyResultSelector => BuildGenericParameterList(descriptor.GenericArity) + ", TElement, TResult",
            TypedStringLambdaKind.Join => BuildGenericParameterList(descriptor.GenericArity) + ", TKey, TResult",
            TypedStringLambdaKind.GroupJoin => BuildGenericParameterList(descriptor.GenericArity) + ", TKey, TResult",
            _ => BuildGenericParameterList(descriptor.GenericArity)
        };

    private static OperatorParseResult? ParseOperatorAttributes(GeneratorAttributeSyntaxContext context)
    {
        var parsed = ImmutableArray.CreateBuilder<OperatorDescriptor>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        foreach (var attribute in context.Attributes)
        {
            if (attribute.AttributeClass?.ToDisplayString() != OperatorAttributeFullName)
                continue;

            var extensionName = ReadStringConstructorArgument(attribute, 0);
            if (!TryParseSourceKinds(ReadNamedString(attribute, "Sources", ""), out var sourceKinds, out var sourceError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, sourceError!));
                continue;
            }

            if (!TryParseUntypedResultKinds(ReadNamedString(attribute, "UntypedResults", ""), out var untypedResults, out var resultError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, resultError!));
                continue;
            }

            if (!TryParseOperatorKind(ReadNamedString(attribute, "DispatcherOperator", ""), out var dispatcherOperator, out var operatorError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, operatorError!));
                continue;
            }

            if (!TryParseProbeType(ReadNamedString(attribute, "ProbeType", ""), out var probeType, out var probeError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, probeError!));
                continue;
            }

            parsed.Add(new OperatorDescriptor(
                extensionName,
                (sourceKinds & SourceKinds.Enumerable) != 0,
                (sourceKinds & SourceKinds.Queryable) != 0,
                (sourceKinds & SourceKinds.Async) != 0,
                (untypedResults & UntypedResultKinds.Sequence) != 0,
                (untypedResults & UntypedResultKinds.Scalar) != 0,
                dispatcherOperator == OperatorKind.None ? null : dispatcherOperator,
                probeType));
        }

        return parsed.Count == 0 && diagnostics.Count == 0
            ? null
            : new OperatorParseResult(parsed.ToImmutable(), diagnostics.ToImmutable());
    }

    private static DispatcherExtensionParseResult? ParseDispatcherExtensionAttributes(GeneratorAttributeSyntaxContext context)
    {
        var parsed = ImmutableArray.CreateBuilder<DispatcherExtensionDescriptor>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        foreach (var attribute in context.Attributes)
        {
            if (attribute.AttributeClass?.ToDisplayString() != DispatcherExtensionAttributeFullName)
                continue;

            var extensionMethodName = ReadStringConstructorArgument(attribute, 0);
            var dispatcherMethodName = ReadStringConstructorArgument(attribute, 1);
            if (!TryParseDispatcherTypeShape(ReadStringConstructorArgument(attribute, 2), "returnType", out var returnType, out var returnTypeError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, returnTypeError!));
                continue;
            }

            if (!TryParseDispatcherTypeShape(ReadStringConstructorArgument(attribute, 3), "sourceType", out var sourceType, out var sourceTypeError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, sourceTypeError!));
                continue;
            }

            var firstExpressionParameter = ReadStringConstructorArgument(attribute, 4);
            if (!TryParseDispatcherTypeShape(ReadNamedString(attribute, "SecondarySourceType", ""), "secondarySourceType", out var secondarySourceType, out var secondarySourceTypeError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, secondarySourceTypeError!));
                continue;
            }

            var secondarySourceName = ReadNamedString(attribute, "SecondarySourceName", "inner");
            var secondExpressionParameter = ReadNamedString(attribute, "SecondExpressionParameter", "");
            var thirdExpressionParameter = ReadNamedString(attribute, "ThirdExpressionParameter", "");
            var includeEngineOverload = ReadNamedBool(attribute, "IncludeEngineOverload", false);
            var includeTypedResultOverload = ReadNamedBool(attribute, "IncludeTypedResultOverload", false);
            var genericArity = ReadNamedInt(attribute, "GenericArity", 1);
            if (!TryParseSourceKinds(ReadNamedString(attribute, "Sources", ""), out var sourceKinds, out var sourceKindsError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, sourceKindsError!));
                continue;
            }

            if ((sourceKinds & SourceKinds.Async) != 0 && !IsSupportedAsyncDispatcherMethod(dispatcherMethodName))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, "Unsupported async dispatcher method '" + dispatcherMethodName + "'."));
                sourceKinds &= ~SourceKinds.Async;
                if (sourceKinds == SourceKinds.None)
                    continue;
            }

            if (!TryParseSortDirection(ReadNamedString(attribute, "SortDirection", ""), out var sortDirection, out var sortDirectionError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, sortDirectionError!));
                continue;
            }

            if (sourceKinds != SourceKinds.None)
            {
                foreach (var sourceKind in ExpandTypedSourceKinds(sourceKinds))
                {
                    parsed.Add(new DispatcherExtensionDescriptor(
                        extensionMethodName,
                        dispatcherMethodName,
                        ResolveSourceSpecificShape(returnType, sourceKind),
                        ResolveSourceSpecificShape(sourceType, sourceKind),
                        ResolveSourceSpecificShape(secondarySourceType, sourceKind),
                        secondarySourceName,
                        firstExpressionParameter,
                        secondExpressionParameter,
                        thirdExpressionParameter,
                        includeEngineOverload,
                        includeTypedResultOverload,
                        genericArity,
                        sortDirection,
                        sourceKind));
                }

                continue;
            }

            parsed.Add(new DispatcherExtensionDescriptor(
                extensionMethodName,
                dispatcherMethodName,
                returnType,
                sourceType,
                secondarySourceType,
                secondarySourceName,
                firstExpressionParameter,
                secondExpressionParameter,
                thirdExpressionParameter,
                includeEngineOverload,
                includeTypedResultOverload,
                genericArity,
                sortDirection,
                SourceKinds.None));
        }

        return parsed.Count == 0 && diagnostics.Count == 0
            ? null
            : new DispatcherExtensionParseResult(parsed.ToImmutable(), diagnostics.ToImmutable());
    }

    private static TypedStringExtensionParseResult? ParseTypedStringExtensionAttributes(GeneratorAttributeSyntaxContext context)
    {
        var parsed = ImmutableArray.CreateBuilder<TypedStringExtensionDescriptor>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        foreach (var attribute in context.Attributes)
        {
            if (attribute.AttributeClass?.ToDisplayString() != TypedStringExtensionAttributeFullName)
                continue;

            var extensionMethodName = ReadStringConstructorArgument(attribute, 0);
            var linqMethodName = ReadStringConstructorArgument(attribute, 1);
            if (!TryParseDispatcherTypeShape(ReadStringConstructorArgument(attribute, 2), "returnType", out var returnType, out var returnTypeError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, returnTypeError!));
                continue;
            }

            if (!TryParseDispatcherTypeShape(ReadStringConstructorArgument(attribute, 3), "sourceType", out var sourceType, out var sourceTypeError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, sourceTypeError!));
                continue;
            }

            if (!TryParseTypedStringLambdaKind(ReadStringConstructorArgument(attribute, 4), out var lambdaKind, out var lambdaKindError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, lambdaKindError!));
                continue;
            }

            var firstExpressionParameter = ReadStringConstructorArgument(attribute, 5);
            if (!TryParseDispatcherTypeShape(ReadNamedString(attribute, "SecondarySourceType", ""), "secondarySourceType", out var secondarySourceType, out var secondarySourceTypeError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, secondarySourceTypeError!));
                continue;
            }

            var secondarySourceName = ReadNamedString(attribute, "SecondarySourceName", "inner");
            var secondExpressionParameter = ReadNamedString(attribute, "SecondExpressionParameter", "");
            var thirdExpressionParameter = ReadNamedString(attribute, "ThirdExpressionParameter", "");
            var includeEngineOverload = ReadNamedBool(attribute, "IncludeEngineOverload", false);
            var genericArity = ReadNamedInt(attribute, "GenericArity", 1);
            if (!TryParseSortDirection(ReadNamedString(attribute, "SortDirection", ""), out var sortDirection, out var sortDirectionError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, sortDirectionError!));
                continue;
            }

            if (!TryParseSourceKinds(ReadNamedString(attribute, "Sources", ""), out var sourceKinds, out var sourceKindsError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, sourceKindsError!));
                continue;
            }

            if ((sourceKinds & SourceKinds.Async) != 0 && !IsSupportedAsyncTypedStringMethod(linqMethodName, lambdaKind))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, "Unsupported async typed string method '" + linqMethodName + "'."));
                sourceKinds &= ~SourceKinds.Async;
                if (sourceKinds == SourceKinds.None)
                    continue;
            }

            if (sourceKinds == SourceKinds.None)
            {
                parsed.Add(new TypedStringExtensionDescriptor(
                    extensionMethodName,
                    linqMethodName,
                    returnType,
                    sourceType,
                    secondarySourceType,
                    secondarySourceName,
                    lambdaKind,
                    firstExpressionParameter,
                    secondExpressionParameter,
                    thirdExpressionParameter,
                    includeEngineOverload,
                    genericArity,
                    sortDirection,
                    SourceKinds.None));
                continue;
            }

            foreach (var sourceKind in ExpandTypedSourceKinds(sourceKinds))
            {
                parsed.Add(new TypedStringExtensionDescriptor(
                    extensionMethodName,
                    linqMethodName,
                    ResolveSourceSpecificShape(returnType, sourceKind),
                    ResolveSourceSpecificShape(sourceType, sourceKind),
                    ResolveSourceSpecificShape(secondarySourceType, sourceKind),
                    secondarySourceName,
                    lambdaKind,
                    firstExpressionParameter,
                    secondExpressionParameter,
                    thirdExpressionParameter,
                    includeEngineOverload,
                    genericArity,
                    sortDirection,
                    sourceKind));
            }
        }

        return parsed.Count == 0 && diagnostics.Count == 0
            ? null
            : new TypedStringExtensionParseResult(parsed.ToImmutable(), diagnostics.ToImmutable());
    }

    private static ForwardingExtensionParseResult? ParseForwardingExtensionAttributes(GeneratorAttributeSyntaxContext context)
    {
        var parsed = ImmutableArray.CreateBuilder<ForwardingExtensionDescriptor>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        foreach (var attribute in context.Attributes)
        {
            if (attribute.AttributeClass?.ToDisplayString() != ForwardingExtensionAttributeFullName)
                continue;

            var extensionMethodName = ReadStringConstructorArgument(attribute, 0);
            var linqMethodName = ReadStringConstructorArgument(attribute, 1);
            if (!TryParseDispatcherTypeShape(ReadStringConstructorArgument(attribute, 2), "returnType", out var returnType, out var returnTypeError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, returnTypeError!));
                continue;
            }

            if (!TryParseDispatcherTypeShape(ReadStringConstructorArgument(attribute, 3), "sourceType", out var sourceType, out var sourceTypeError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, sourceTypeError!));
                continue;
            }

            var genericParameters = ReadStringConstructorArgument(attribute, 4);
            if (!TryParseSourceKinds(ReadNamedString(attribute, "Sources", ""), out var sourceKinds, out var sourceKindsError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, sourceKindsError!));
                continue;
            }

            if (!TryParseDispatcherTypeShape(ReadNamedString(attribute, "SecondarySourceType", ""), "secondarySourceType", out var secondarySourceType, out var secondarySourceTypeError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, secondarySourceTypeError!));
                continue;
            }

            if (!TryParseDispatcherTypeShape(ReadNamedString(attribute, "ValueParameterType", ""), "valueParameterType", out var valueParameterType, out var valueParameterTypeError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, valueParameterTypeError!));
                continue;
            }

            var secondarySourceName = ReadNamedString(attribute, "SecondarySourceName", "second");
            var valueParameterName = ReadNamedString(attribute, "ValueParameterName", "");
            var nullForgivingResult = ReadNamedBool(attribute, "NullForgivingResult", false);
            if ((sourceKinds & SourceKinds.Async) != 0 && !IsSupportedAsyncForwardingMethod(linqMethodName))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, "Unsupported async forwarding method '" + linqMethodName + "'."));
                sourceKinds &= ~SourceKinds.Async;
                if (sourceKinds == SourceKinds.None)
                    continue;
            }

            foreach (var sourceKind in ExpandTypedSourceKinds(sourceKinds))
            {
                parsed.Add(new ForwardingExtensionDescriptor(
                    extensionMethodName,
                    linqMethodName,
                    ResolveSourceSpecificShape(returnType, sourceKind),
                    ResolveSourceSpecificShape(sourceType, sourceKind),
                    ResolveSourceSpecificShape(secondarySourceType, sourceKind),
                    secondarySourceName,
                    valueParameterType,
                    valueParameterName,
                    genericParameters,
                    GetGenericArity(genericParameters),
                    sourceKind,
                    nullForgivingResult));
            }
        }

        return parsed.Count == 0 && diagnostics.Count == 0
            ? null
            : new ForwardingExtensionParseResult(parsed.ToImmutable(), diagnostics.ToImmutable());
    }

    private static LambdaForwardingExtensionParseResult? ParseLambdaForwardingExtensionAttributes(GeneratorAttributeSyntaxContext context)
    {
        var parsed = ImmutableArray.CreateBuilder<LambdaForwardingExtensionDescriptor>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        foreach (var attribute in context.Attributes)
        {
            if (attribute.AttributeClass?.ToDisplayString() != LambdaForwardingExtensionAttributeFullName)
                continue;

            var extensionMethodName = ReadStringConstructorArgument(attribute, 0);
            var linqMethodName = ReadStringConstructorArgument(attribute, 1);
            if (!TryParseDispatcherTypeShape(ReadStringConstructorArgument(attribute, 2), "returnType", out var returnType, out var returnTypeError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, returnTypeError!));
                continue;
            }

            if (!TryParseDispatcherTypeShape(ReadStringConstructorArgument(attribute, 3), "sourceType", out var sourceType, out var sourceTypeError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, sourceTypeError!));
                continue;
            }

            var genericParameters = ReadStringConstructorArgument(attribute, 4);
            if (!TryParseLambdaForwardingKind(ReadStringConstructorArgument(attribute, 5), out var lambdaKind, out var lambdaKindError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, lambdaKindError!));
                continue;
            }

            var lambdaParameterName = ReadStringConstructorArgument(attribute, 6);
            if (!TryParseSourceKinds(ReadNamedString(attribute, "Sources", ""), out var sourceKinds, out var sourceKindsError))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, sourceKindsError!));
                continue;
            }

            if ((sourceKinds & SourceKinds.Async) != 0 && !IsSupportedAsyncLambdaForwardingMethod(linqMethodName, lambdaKind))
            {
                diagnostics.Add(CreateMetadataDiagnostic(attribute, "Unsupported async lambda forwarding method '" + linqMethodName + "'."));
                sourceKinds &= ~SourceKinds.Async;
                if (sourceKinds == SourceKinds.None)
                    continue;
            }

            foreach (var sourceKind in ExpandTypedSourceKinds(sourceKinds))
            {
                parsed.Add(new LambdaForwardingExtensionDescriptor(
                    extensionMethodName,
                    linqMethodName,
                    ResolveSourceSpecificShape(returnType, sourceKind),
                    ResolveSourceSpecificShape(sourceType, sourceKind),
                    genericParameters,
                    BuildLinqGenericParameters(genericParameters, lambdaKind),
                    GetGenericArity(genericParameters),
                    lambdaKind,
                    lambdaParameterName,
                    sourceKind));
            }
        }

        return parsed.Count == 0 && diagnostics.Count == 0
            ? null
            : new LambdaForwardingExtensionParseResult(parsed.ToImmutable(), diagnostics.ToImmutable());
    }

    private static IEnumerable<SourceKinds> ExpandTypedSourceKinds(SourceKinds sourceKinds)
    {
        if ((sourceKinds & SourceKinds.Enumerable) != 0)
            yield return SourceKinds.Enumerable;
        if ((sourceKinds & SourceKinds.Queryable) != 0)
            yield return SourceKinds.Queryable;
        if ((sourceKinds & SourceKinds.Async) != 0)
            yield return SourceKinds.Async;
    }

    private static DispatcherTypeShape ResolveSourceSpecificShape(DispatcherTypeShape shape, SourceKinds sourceKind) =>
        sourceKind switch
        {
            SourceKinds.Enumerable => shape switch
            {
                DispatcherTypeShape.SequenceOfT => DispatcherTypeShape.IEnumerableOfT,
                DispatcherTypeShape.SequenceOfTResult => DispatcherTypeShape.IEnumerableOfTResult,
                DispatcherTypeShape.SequenceOfTElement => DispatcherTypeShape.IEnumerableOfTElement,
                DispatcherTypeShape.SequenceOfTSecond => DispatcherTypeShape.IEnumerableOfTSecond,
                DispatcherTypeShape.OrderedSequenceOfT => DispatcherTypeShape.IOrderedEnumerableOfT,
                DispatcherTypeShape.SequenceOfGrouping => DispatcherTypeShape.IEnumerableOfGrouping,
                _ => shape
            },
            SourceKinds.Queryable => shape switch
            {
                DispatcherTypeShape.SequenceOfT => DispatcherTypeShape.IQueryableOfT,
                DispatcherTypeShape.SequenceOfTResult => DispatcherTypeShape.IQueryableOfTResult,
                DispatcherTypeShape.SequenceOfTElement => DispatcherTypeShape.IQueryableOfTElement,
                DispatcherTypeShape.SequenceOfTSecond => DispatcherTypeShape.IQueryableOfTSecond,
                DispatcherTypeShape.OrderedSequenceOfT => DispatcherTypeShape.IOrderedQueryableOfT,
                DispatcherTypeShape.SequenceOfGrouping => DispatcherTypeShape.IQueryableOfGrouping,
                _ => shape
            },
            SourceKinds.Async => shape switch
            {
                DispatcherTypeShape.SequenceOfT => DispatcherTypeShape.IAsyncEnumerableOfT,
                _ => shape
            },
            _ => shape
        };

    private static bool IsSupportedAsyncDispatcherMethod(string methodName) =>
        methodName is "Select" or "SelectMany" or "Sum" or "Average" or "Min" or "Max";

    private static bool IsSupportedAsyncForwardingMethod(string methodName) =>
        methodName is "Skip" or "Take" or "Distinct" or "Reverse";

    private static bool IsSupportedAsyncTypedStringMethod(string methodName, TypedStringLambdaKind lambdaKind) =>
        lambdaKind switch
        {
            TypedStringLambdaKind.Predicate => methodName is
                "Where" or
                "Any" or
                "All" or
                "Count" or
                "LongCount" or
                "First" or
                "FirstOrDefault" or
                "Last" or
                "LastOrDefault" or
                "Single" or
                "SingleOrDefault" or
                "SkipWhile" or
                "TakeWhile",
            TypedStringLambdaKind.MaterializingSelector => methodName == "Select",
            TypedStringLambdaKind.CollectionSelector => methodName == "SelectMany",
            _ => false
        };

    private static bool IsSupportedAsyncLambdaForwardingMethod(string methodName, LambdaForwardingKind lambdaKind) =>
        lambdaKind switch
        {
            LambdaForwardingKind.ExpressionPredicate or LambdaForwardingKind.FuncPredicate =>
                methodName is "Where" or "Any" or "Count",
            LambdaForwardingKind.ExpressionSelector or LambdaForwardingKind.FuncSelector =>
                methodName == "Select",
            LambdaForwardingKind.ExpressionCollectionSelector or LambdaForwardingKind.FuncCollectionSelector =>
                methodName == "SelectMany",
            LambdaForwardingKind.ExpressionDecimalSelector or LambdaForwardingKind.FuncDecimalSelector =>
                methodName == "Sum",
            _ => false
        };

    private static int GetGenericArity(string genericParameters) =>
        string.IsNullOrWhiteSpace(genericParameters)
            ? 0
            : genericParameters.Split(',').Length;

    private static string BuildLinqGenericParameters(string genericParameters, LambdaForwardingKind lambdaKind)
    {
        if (lambdaKind is LambdaForwardingKind.ExpressionKeySelector or LambdaForwardingKind.FuncKeySelector)
            return "T, TKey";
        return genericParameters;
    }

    private static string GetOperatorMethodName(OperatorKind op) =>
        op switch
        {
            OperatorKind.Select => "Select",
            OperatorKind.SelectMany => "SelectMany",
            OperatorKind.SelectManyWithResultSelector => "SelectMany",
            OperatorKind.OrderBy => "OrderBy",
            OperatorKind.OrderByDescending => "OrderByDescending",
            OperatorKind.ThenBy => "ThenBy",
            OperatorKind.ThenByDescending => "ThenByDescending",
            OperatorKind.GroupBy => "GroupBy",
            OperatorKind.Join => "Join",
            OperatorKind.GroupJoin => "GroupJoin",
            OperatorKind.Min => "Min",
            OperatorKind.Max => "Max",
            OperatorKind.Sum => "Sum",
            OperatorKind.Average => "Average",
            OperatorKind.Contains => "Contains",
            OperatorKind.ElementAt => "ElementAt",
            OperatorKind.ElementAtOrDefault => "ElementAtOrDefault",
            OperatorKind.DefaultIfEmpty => "DefaultIfEmpty",
            OperatorKind.DefaultIfEmptyWithValue => "DefaultIfEmpty",
            OperatorKind.Append => "Append",
            OperatorKind.Prepend => "Prepend",
            _ => throw new InvalidOperationException("Unsupported dispatcher operator '" + op + "'.")
        };

    private static string GetOperatorMatchExpression(OperatorKind op) =>
        op switch
        {
            OperatorKind.Select => "genericCount == 2 && MatchesUnarySelector(provider, parameters)",
            OperatorKind.SelectMany => "genericCount == 2 && MatchesCollectionSelector(provider, parameters)",
            OperatorKind.SelectManyWithResultSelector => "genericCount == 3 && MatchesSelectManyResultSelector(provider, parameters)",
            OperatorKind.OrderBy => "genericCount == 2 && MatchesUnarySelector(provider, parameters)",
            OperatorKind.OrderByDescending => "genericCount == 2 && MatchesUnarySelector(provider, parameters)",
            OperatorKind.ThenBy => "genericCount == 2 && MatchesOrderedUnarySelector(provider, parameters)",
            OperatorKind.ThenByDescending => "genericCount == 2 && MatchesOrderedUnarySelector(provider, parameters)",
            OperatorKind.GroupBy => "genericCount == 2 && MatchesUnarySelector(provider, parameters)",
            OperatorKind.Join => "genericCount == 4 && MatchesJoin(provider, parameters)",
            OperatorKind.GroupJoin => "genericCount == 4 && MatchesGroupJoin(provider, parameters)",
            OperatorKind.Min => "genericCount == 2 && MatchesUnarySelector(provider, parameters)",
            OperatorKind.Max => "genericCount == 2 && MatchesUnarySelector(provider, parameters)",
            OperatorKind.Sum => "genericCount == 1 && MatchesNumericSelector(provider, parameters, selectorResultType)",
            OperatorKind.Average => "genericCount == 1 && MatchesNumericSelector(provider, parameters, selectorResultType)",
            OperatorKind.Contains => "MatchesContains(provider, parameters)",
            OperatorKind.ElementAt => "MatchesIndexOperator(provider, parameters)",
            OperatorKind.ElementAtOrDefault => "MatchesIndexOperator(provider, parameters)",
            OperatorKind.DefaultIfEmpty => "MatchesDefaultIfEmpty(provider, parameters, hasValue: false)",
            OperatorKind.DefaultIfEmptyWithValue => "MatchesDefaultIfEmpty(provider, parameters, hasValue: true)",
            OperatorKind.Append => "MatchesAppendPrepend(provider, parameters)",
            OperatorKind.Prepend => "MatchesAppendPrepend(provider, parameters)",
            _ => throw new InvalidOperationException("Unsupported dispatcher operator '" + op + "'.")
        };

    private static string ReadStringConstructorArgument(AttributeData attribute, int index)
    {
        if (attribute.ConstructorArguments.Length <= index || attribute.ConstructorArguments[index].Value is not string value)
            return "";
        return value;
    }

    private static string ReadNamedString(AttributeData attribute, string name, string fallback)
    {
        foreach (var argument in attribute.NamedArguments)
            if (argument.Key == name && argument.Value.Value is string value)
                return value;
        return fallback;
    }

    private static bool ReadNamedBool(AttributeData attribute, string name, bool fallback)
    {
        foreach (var argument in attribute.NamedArguments)
            if (argument.Key == name && argument.Value.Value is bool value)
                return value;
        return fallback;
    }

    private static int ReadNamedInt(AttributeData attribute, string name, int fallback)
    {
        foreach (var argument in attribute.NamedArguments)
            if (argument.Key == name && argument.Value.Value is int value)
                return value;
        return fallback;
    }

    private static string ToStringLiteral(string value) =>
        "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    private readonly record struct OperatorParseResult(
        ImmutableArray<OperatorDescriptor> Items,
        ImmutableArray<Diagnostic> Diagnostics);
    private readonly record struct DispatcherExtensionParseResult(
        ImmutableArray<DispatcherExtensionDescriptor> Items,
        ImmutableArray<Diagnostic> Diagnostics);
    private readonly record struct TypedStringExtensionParseResult(
        ImmutableArray<TypedStringExtensionDescriptor> Items,
        ImmutableArray<Diagnostic> Diagnostics);
    private readonly record struct ForwardingExtensionParseResult(
        ImmutableArray<ForwardingExtensionDescriptor> Items,
        ImmutableArray<Diagnostic> Diagnostics);
    private readonly record struct LambdaForwardingExtensionParseResult(
        ImmutableArray<LambdaForwardingExtensionDescriptor> Items,
        ImmutableArray<Diagnostic> Diagnostics);

    private readonly record struct OperatorDescriptor(
        string ExtensionName,
        bool RequireEnumerableSource,
        bool RequireQueryableSource,
        bool RequireAsyncSource,
        bool RequireUntypedSequenceResult,
        bool RequireUntypedScalarResult,
        OperatorKind? DispatcherOperatorKind,
        ProbeType DispatcherProbeType);

    private readonly record struct DispatcherExtensionDescriptor(
        string ExtensionMethodName,
        string DispatcherMethodName,
        DispatcherTypeShape ReturnType,
        DispatcherTypeShape SourceType,
        DispatcherTypeShape SecondarySourceType,
        string SecondarySourceName,
        string FirstExpressionParameter,
        string SecondExpressionParameter,
        string ThirdExpressionParameter,
        bool IncludeEngineOverload,
        bool IncludeTypedResultOverload,
        int GenericArity,
        SortDirection SortDirection,
        SourceKinds SourceKind);

    private readonly record struct TypedStringExtensionDescriptor(
        string ExtensionMethodName,
        string LinqMethodName,
        DispatcherTypeShape ReturnType,
        DispatcherTypeShape SourceType,
        DispatcherTypeShape SecondarySourceType,
        string SecondarySourceName,
        TypedStringLambdaKind LambdaKind,
        string FirstExpressionParameter,
        string SecondExpressionParameter,
        string ThirdExpressionParameter,
        bool IncludeEngineOverload,
        int GenericArity,
        SortDirection SortDirection,
        SourceKinds SourceKind);

    private readonly record struct ForwardingExtensionDescriptor(
        string ExtensionMethodName,
        string LinqMethodName,
        DispatcherTypeShape ReturnType,
        DispatcherTypeShape SourceType,
        DispatcherTypeShape SecondarySourceType,
        string SecondarySourceName,
        DispatcherTypeShape ValueParameterType,
        string ValueParameterName,
        string GenericParameters,
        int GenericArity,
        SourceKinds SourceKind,
        bool NullForgivingResult);

    private readonly record struct LambdaForwardingExtensionDescriptor(
        string ExtensionMethodName,
        string LinqMethodName,
        DispatcherTypeShape ReturnType,
        DispatcherTypeShape SourceType,
        string GenericParameters,
        string LinqGenericParameters,
        int GenericArity,
        LambdaForwardingKind LambdaKind,
        string LambdaParameterName,
        SourceKinds SourceKind);

    [Flags]
    private enum SourceKinds
    {
        None = 0,
        Enumerable = 1 << 0,
        Queryable = 1 << 1,
        Async = 1 << 2
    }

    [Flags]
    private enum UntypedResultKinds
    {
        None = 0,
        Sequence = 1 << 0,
        Scalar = 1 << 1
    }

    private enum ProbeType
    {
        None = 0,
        Boolean,
        Int32,
        Int64,
        Decimal,
        String,
        Object
    }

    private enum OperatorKind
    {
        None = 0,
        Select,
        SelectMany,
        SelectManyWithResultSelector,
        OrderBy,
        OrderByDescending,
        ThenBy,
        ThenByDescending,
        GroupBy,
        Join,
        GroupJoin,
        Min,
        Max,
        Sum,
        Average,
        Contains,
        ElementAt,
        ElementAtOrDefault,
        DefaultIfEmpty,
        DefaultIfEmptyWithValue,
        Append,
        Prepend
    }

    private enum DispatcherTypeShape
    {
        None,
        Sequence,
        IEnumerable,
        IQueryable,
        IEnumerableOfT,
        IQueryableOfT,
        IAsyncEnumerableOfT,
        SequenceOfT,
        IEnumerableOfTResult,
        IQueryableOfTResult,
        SequenceOfTResult,
        IEnumerableOfTElement,
        IQueryableOfTElement,
        SequenceOfTElement,
        IEnumerableOfTSecond,
        IQueryableOfTSecond,
        SequenceOfTSecond,
        IEnumerableOfGrouping,
        IQueryableOfGrouping,
        SequenceOfGrouping,
        IOrderedEnumerableOfT,
        IOrderedQueryableOfT,
        OrderedSequenceOfT,
        Object,
        Boolean,
        Int32,
        Int64,
        Decimal,
        T,
        NullableT
    }

    private enum TypedStringLambdaKind
    {
        Predicate,
        Selector,
        MaterializingSelector,
        CollectionSelector,
        SelectManyResultSelector,
        Grouping,
        Join,
        GroupJoin
    }

    private enum LambdaForwardingKind
    {
        ExpressionPredicate,
        FuncPredicate,
        ExpressionSelector,
        FuncSelector,
        ExpressionKeySelector,
        FuncKeySelector,
        ExpressionCollectionSelector,
        FuncCollectionSelector,
        ExpressionDecimalSelector,
        FuncDecimalSelector
    }

    private enum SortDirection
    {
        None = 0,
        Ascending = 1,
        Descending = 2
    }

    private static bool TryParseSourceKinds(string value, out SourceKinds result, out string? error)
    {
        result = SourceKinds.None;
        error = null;
        foreach (var token in SplitFlags(value))
        {
            switch (token)
            {
                case "Enumerable":
                    result |= SourceKinds.Enumerable;
                    break;
                case "Queryable":
                    result |= SourceKinds.Queryable;
                    break;
                case "Async":
                    result |= SourceKinds.Async;
                    break;
                default:
                    error = "Unsupported Sources token '" + token + "'.";
                    return false;
            }
        }

        return true;
    }

    private static bool TryParseUntypedResultKinds(string value, out UntypedResultKinds result, out string? error)
    {
        result = UntypedResultKinds.None;
        error = null;
        foreach (var token in SplitFlags(value))
        {
            switch (token)
            {
                case "Sequence":
                    result |= UntypedResultKinds.Sequence;
                    break;
                case "Scalar":
                    result |= UntypedResultKinds.Scalar;
                    break;
                default:
                    error = "Unsupported UntypedResults token '" + token + "'.";
                    return false;
            }
        }

        return true;
    }

    private static bool TryParseOperatorKind(string value, out OperatorKind result, out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = OperatorKind.None;
            error = null;
            return true;
        }

        if (Enum.TryParse<OperatorKind>(value, ignoreCase: false, out result))
        {
            error = null;
            return true;
        }

        error = "Unsupported DispatcherOperator token '" + value + "'.";
        return false;
    }

    private static bool TryParseProbeType(string value, out ProbeType result, out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = ProbeType.None;
            error = null;
            return true;
        }

        if (Enum.TryParse<ProbeType>(value, ignoreCase: false, out result))
        {
            error = null;
            return true;
        }

        error = "Unsupported ProbeType token '" + value + "'.";
        return false;
    }

    private static bool TryParseDispatcherTypeShape(
        string value,
        string argumentName,
        out DispatcherTypeShape result,
        out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = DispatcherTypeShape.None;
            error = null;
            return true;
        }

        if (Enum.TryParse<DispatcherTypeShape>(value, ignoreCase: false, out result))
        {
            error = null;
            return true;
        }

        error = "Unsupported " + argumentName + " token '" + value + "'.";
        return false;
    }

    private static bool TryParseSortDirection(string value, out SortDirection result, out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = SortDirection.None;
            error = null;
            return true;
        }

        if (Enum.TryParse<SortDirection>(value, ignoreCase: false, out result))
        {
            error = null;
            return true;
        }

        error = "Unsupported SortDirection token '" + value + "'.";
        return false;
    }

    private static bool TryParseTypedStringLambdaKind(string value, out TypedStringLambdaKind result, out string? error)
    {
        if (Enum.TryParse<TypedStringLambdaKind>(value, ignoreCase: false, out result))
        {
            error = null;
            return true;
        }

        error = "Unsupported lambdaKind token '" + value + "'.";
        return false;
    }

    private static bool TryParseLambdaForwardingKind(string value, out LambdaForwardingKind result, out string? error)
    {
        if (Enum.TryParse<LambdaForwardingKind>(value, ignoreCase: false, out result))
        {
            error = null;
            return true;
        }

        error = "Unsupported lambdaKind token '" + value + "'.";
        return false;
    }

    private static IEnumerable<string> SplitFlags(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Enumerable.Empty<string>()
            : value.Split('|').Select(static part => part.Trim()).Where(static part => part.Length > 0);

    private static Diagnostic CreateMetadataDiagnostic(AttributeData attribute, string message)
    {
        var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
        return Diagnostic.Create(InvalidMetadataToken, location, message);
    }

}
