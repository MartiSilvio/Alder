using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(MultiDimArrayInitExpr))]
internal static class MultiDimArrayInitBinder
{
    public static BoundExpr Bind(MultiDimArrayInitExpr expr, BindingContext context, BinderContext binder)
    {
        var explicitSizes = expr.ExplicitSizes?
            .Select(size => binder.Bind(size, context))
            .ToImmutableArray();
        var flatValues = expr.FlatValues
            .Select(value => binder.Bind(value, context))
            .ToImmutableArray();
        var elementType = context.RuntimeContext.TypeResolver.TryResolveType(expr.ElementTypeName) ?? typeof(object);
        var arrayType = RuntimeArrayFactory.GetArrayType(elementType, expr.Rank);
        return new BoundMultiDimArrayInitExpr(
            elementType,
            expr.Rank,
            explicitSizes,
            flatValues,
            expr.InferredDimensions,
            new BoundType(arrayType));
    }
}
