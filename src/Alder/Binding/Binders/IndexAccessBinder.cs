using Alder.Binding.BoundNodes;
using Alder.Binding.Services;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(IndexAccessExpr))]
internal static class IndexAccessBinder
{
    public static BoundExpr Bind(IndexAccessExpr expr, BindingContext context, BinderContext binder)
    {
        var target = binder.Bind(expr.Object, context);
        var index = binder.Bind(expr.Index, context);

        var memberBinder = new MemberBinderService(context.RuntimeContext.TypeMetadata);
        if (memberBinder.TryBindIndexRead(target.StaticType.ClrType, index.StaticType.ClrType, out var indexPlan))
        {
            return new BoundResolvedIndexAccessExpr(
                target, index,
                indexPlan!.TargetType, indexPlan.ResultType, indexPlan.IsDirectCollectionAccess,
                expr.NullSafe, new BoundType(indexPlan.ResultType));
        }

        return new BoundDynamicIndexAccessExpr(target, index, expr.NullSafe, BoundType.Unknown);
    }
}
