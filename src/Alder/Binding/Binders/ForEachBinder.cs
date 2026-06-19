using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(ForEachStatementExpr))]
internal static class ForEachBinder
{
    public static BoundExpr Bind(ForEachStatementExpr expr, BindingContext context, BinderContext binder)
    {
        var collection = binder.Bind(expr.Collection, context);
        var inferredElementType = InferElementType(collection.StaticType.ClrType);

        // §13.9.5: explicit iteration type must accept the collection's element type.
        // An unknown collection type (e.g. `object` or `dynamic`) defers the check to runtime.
        var iterationType = inferredElementType;
        if (expr.DeclaredTypeName != null)
        {
            iterationType = context.RuntimeContext.TypeResolver.ResolveType(expr.DeclaredTypeName);
            if (!collection.HasErrors
                && collection.StaticType is not BoundUnknownType
                && collection.StaticType.ClrType != typeof(object)
                && inferredElementType != typeof(object)
                && iterationType != inferredElementType
                && !TypeHelpers.HasExplicitConversion(inferredElementType, iterationType))
            {
                // Roslyn reports a foreach element-type mismatch as CS0030 (an explicit cast
                // would be required inside the loop), not CS0029, because foreach treats the
                // declared variable type as an explicit conversion target.
                throw new AlderException(
                    DiagnosticDescriptors.NoExplicitConversion,
                    inferredElementType.Name,
                    iterationType.Name);
            }
        }

        var loopFlags = BinderFlags.InLoop;
        if (binder.Includes(BinderFlags.InFinally))
            loopFlags |= BinderFlags.InFinallyLoop;
        var loopBinder = binder.WithAdditionalFlags(loopFlags);
        var bodyScope = context.CreateChildScope();
        // §13.9.5: the iteration variable is readonly within the loop body.
        var foreachLocalId = bodyScope.DeclareLocal(expr.VariableName.Lexeme, new BoundType(iterationType), ReadOnlyReason.IterationVariable);
        var body = expr.Body
            .Select(statement => loopBinder.Bind(statement, bodyScope))
            .ToImmutableArray();
        return new BoundForEachExpr(
            expr.VariableName.Lexeme,
            collection,
            body,
            iterationType,
            BoundType.Void,
            foreachLocalId,
            inferredElementType);
    }

    internal static Type InferElementType(Type collectionType)
    {
        if (collectionType.IsArray)
            return collectionType.GetElementType()!;

        return TryGetEnumerableInterface(collectionType)?.GetGenericArguments()[0] ?? typeof(object);
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2067",
        Justification = "Foreach element inference is a reflection compatibility path; NativeAOT hosts must use registered/generated types whose interface metadata is rooted.")]
    private static Type? TryGetEnumerableInterface(Type collectionType)
    {
        if (collectionType.IsArray)
            return null;

        if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return collectionType;

        foreach (var iface in RuntimeTypeIntrospection.GetInterfaces(collectionType))
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return iface;
        }

        return null;
    }
}
