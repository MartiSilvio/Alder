using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(TypedArrayCreationExpr))]
internal static class TypedArrayCreationBinder
{
    public static BoundExpr Bind(TypedArrayCreationExpr expr, BindingContext context, BinderContext binder)
    {
        var size = binder.Bind(expr.Size, context);
        var elementType = context.RuntimeContext.TypeResolver.TryResolveType(expr.ElementTypeName)
            ?? throw new AlderException(DiagnosticDescriptors.TypeNotFound, expr.ElementTypeName);
        var arrayType = RuntimeArrayFactory.GetArrayType(elementType);
        return new BoundArrayAllocationExpr(elementType, [size], new BoundType(arrayType));
    }
}
