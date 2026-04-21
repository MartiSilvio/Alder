using System.Collections.Immutable;
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
                && !TypeHelpers.CanImplicitlyConvert(inferredElementType, iterationType))
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

        var loopBinder = binder.WithAdditionalFlags(BinderFlags.InLoop);
        var bodyScope = context.CreateChildScope();
        // §13.9.5: the iteration variable is readonly within the loop body.
        var foreachLocalId = bodyScope.DeclareLocal(expr.VariableName.Lexeme, new BoundType(iterationType), ReadOnlyReason.IterationVariable);
        var body = expr.Body
            .Select(statement => loopBinder.Bind(statement, bodyScope))
            .ToImmutableArray();
        return new BoundForEachExpr(expr.VariableName.Lexeme, collection, body, iterationType, BoundType.Void, foreachLocalId);
    }

    internal static Type InferElementType(Type collectionType)
    {
        if (collectionType.IsArray)
            return collectionType.GetElementType()!;

        foreach (var iface in RuntimeTypeIntrospection.GetInterfaces(collectionType))
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return iface.GetGenericArguments()[0];
        }

        if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return collectionType.GetGenericArguments()[0];

        return typeof(object);
    }
}
