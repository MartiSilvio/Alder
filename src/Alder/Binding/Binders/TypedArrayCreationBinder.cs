using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

internal sealed class TypedArrayCreationBinder : INodeBinder<TypedArrayCreationExpr>
{
    public BoundExpr Bind(TypedArrayCreationExpr expr, BindingContext context, BinderContext binder)
    {
        var size = binder.Bind(expr.Size, context);
        var elementType = context.RuntimeContext.TypeResolver.TryResolveType(expr.ElementTypeName) ?? typeof(object);
        var arrayType = RuntimeArrayFactory.GetArrayType(elementType);
        return new BoundArrayAllocationExpr(elementType, [size], new BoundType(arrayType));
    }
}
