using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(SliceExpr))]
internal static class SliceBinder
{
    public static BoundExpr Bind(SliceExpr expr, BindingContext context, BinderContext binder)
    {
        var target = binder.Bind(expr.Target, context);
        var start = expr.Start != null ? binder.Bind(expr.Start, context) : null;
        var end = expr.End != null ? binder.Bind(expr.End, context) : null;
        var step = expr.Step != null ? binder.Bind(expr.Step, context) : null;
        var sliceType = InferSliceType(target.StaticType.ClrType);
        return new BoundSliceExpr(target, start, end, step, new BoundType(sliceType));
    }

    private static Type InferSliceType(Type targetType)
    {
        if (targetType == typeof(string))
            return typeof(string);

        if (targetType.IsArray)
            return targetType;

        return typeof(object);
    }
}
