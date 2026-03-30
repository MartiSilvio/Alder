using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(IndexNullCoalesceAssignExpr))]
internal static class IndexNullCoalesceAssignBinder
{
    public static BoundExpr Bind(IndexNullCoalesceAssignExpr expr, BindingContext context, BinderContext binder)
    {
        var target = binder.Bind(expr.Object, context);
        var index = binder.Bind(expr.Index, context);
        var value = binder.Bind(expr.Value, context);
        var indexResult = IndexCompoundAssignBinder.ResolveIndexPlan(target.StaticType.ClrType, index.StaticType.ClrType, context);
        var elementType = indexResult?.ResultType ?? IndexCompoundAssignBinder.ResolveIndexElementTypeFallback(target.StaticType.ClrType);
        return new BoundIndexNullCoalesceAssignExpr(target, index, value, new BoundType(elementType));
    }
}
