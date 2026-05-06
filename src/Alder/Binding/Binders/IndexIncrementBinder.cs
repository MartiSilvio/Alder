using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(IndexIncrementExpr))]
internal static class IndexIncrementBinder
{
    public static BoundExpr Bind(IndexIncrementExpr expr, BindingContext context, BinderContext binder)
    {
        var target = binder.Bind(expr.Object, context);
        var index = binder.Bind(expr.Index, context);
        var indexResult = IndexCompoundAssignBinder.ResolveIndexPlan(target.StaticType.ClrType, index.StaticType.ClrType, context);
        var elementType = indexResult?.ResultType ?? IndexCompoundAssignBinder.ResolveIndexElementTypeFallback(target.StaticType.ClrType);
        return new BoundIndexIncrementExpr(
            target,
            index,
            expr.IsPrefix,
            expr.IsIncrement,
            new BoundType(elementType));
    }
}
