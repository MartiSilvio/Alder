using Alder.Binding.BoundNodes;
using Alder.Binding.Services;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class IndexCompoundAssignBinder : INodeBinder<IndexCompoundAssignExpr>
{
    public BoundExpr Bind(IndexCompoundAssignExpr expr, BindingContext context, BinderContext binder)
    {
        var target = binder.Bind(expr.Object, context);
        var index = binder.Bind(expr.Index, context);
        var value = binder.Bind(expr.Value, context);
        var indexResult = ResolveIndexPlan(target.StaticType.ClrType, index.StaticType.ClrType, context);
        var elementType = indexResult?.ResultType ?? ResolveIndexElementTypeFallback(target.StaticType.ClrType);
        var staticType = elementType != typeof(object)
            ? BinaryBinder.InferBinaryResultType(expr.Operator, new BoundType(elementType), value.StaticType)
            : typeof(object);
        return new BoundIndexCompoundAssignExpr(target, index, expr.Operator, value, new BoundType(staticType));
    }

    internal static IndexBindResult? ResolveIndexPlan(Type targetType, Type indexType, BindingContext context)
    {
        if (targetType == typeof(object))
            return null;

        var memberBinder = new MemberBinderService(context.RuntimeContext.TypeMetadata);
        return memberBinder.TryBindIndexRead(targetType, indexType, out var result)
            ? result
            : null;
    }

    internal static Type ResolveIndexElementTypeFallback(Type targetType) =>
        targetType == typeof(object) ? typeof(object) : ForEachBinder.InferElementType(targetType);
}
