using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(TypedArrayLiteralExpr))]
internal static class TypedArrayLiteralBinder
{
    public static BoundExpr Bind(TypedArrayLiteralExpr expr, BindingContext context, BinderContext binder)
    {
        var elements = expr.Elements
            .Select(element => binder.Bind(element, context))
            .ToImmutableArray();
        var elementType = context.RuntimeContext.TypeResolver.TryResolveType(expr.ElementTypeName) ?? typeof(object);
        var arrayType = RuntimeArrayFactory.GetArrayType(elementType);
        return new BoundCollectionCreationExpr(elements, elementType, CollectionKind.Array, null, new BoundType(arrayType));
    }
}
