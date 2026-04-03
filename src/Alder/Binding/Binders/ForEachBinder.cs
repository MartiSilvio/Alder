using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(ForEachStatementExpr))]
internal static class ForEachBinder
{
    public static BoundExpr Bind(ForEachStatementExpr expr, BindingContext context, BinderContext binder)
    {
        var collection = binder.Bind(expr.Collection, context);
        var elementType = InferElementType(collection.StaticType.ClrType);
        var loopBinder = binder.WithAdditionalFlags(BinderFlags.InLoop);
        var bodyScope = context.CreateChildScope();
        var foreachLocalId = bodyScope.DeclareLocal(expr.VariableName.Lexeme, new BoundType(elementType));
        var body = expr.Body
            .Select(statement => loopBinder.Bind(statement, bodyScope))
            .ToImmutableArray();
        return new BoundForEachExpr(expr.VariableName.Lexeme, collection, body, elementType, BoundType.Void, foreachLocalId);
    }

    internal static Type InferElementType(Type collectionType)
    {
        if (collectionType.IsArray)
            return collectionType.GetElementType()!;

        foreach (var iface in collectionType.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return iface.GetGenericArguments()[0];
        }

        if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return collectionType.GetGenericArguments()[0];

        return typeof(object);
    }
}
