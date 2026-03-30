using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(MultiDimTypedArrayCreationExpr))]
internal static class MultiDimTypedArrayCreationBinder
{
    public static BoundExpr Bind(MultiDimTypedArrayCreationExpr expr, BindingContext context, BinderContext binder)
    {
        var sizes = expr.Sizes
            .Select(size => binder.Bind(size, context))
            .ToImmutableArray();
        var elementType = context.RuntimeContext.TypeResolver.TryResolveType(expr.ElementTypeName)
            ?? throw new AlderException(DiagnosticDescriptors.TypeNotFound, expr.ElementTypeName);
        var arrayType = RuntimeArrayFactory.GetArrayType(elementType, expr.Sizes.Count);
        return new BoundArrayAllocationExpr(elementType, sizes, new BoundType(arrayType));
    }
}
