using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(MultiDimIndexAccessExpr))]
internal static class MultiDimIndexAccessBinder
{
    public static BoundExpr Bind(MultiDimIndexAccessExpr expr, BindingContext context, BinderContext binder)
    {
        var target = binder.Bind(expr.Object, context);
        var indices = expr.Indices
            .Select(index => binder.Bind(index, context))
            .ToImmutableArray();
        var targetType = target.StaticType.ClrType;
        var (resolved, elementType) = TryBindMultiDimIndex(targetType, indices.Length, context);

        if (resolved != null)
            return new BoundResolvedMultiDimIndexAccessExpr(
                target, indices, resolved.Value.TargetType, resolved.Value.IsArray, resolved.Value.Indexer,
                expr.NullSafe, new BoundType(elementType));

        return new BoundDynamicMultiDimIndexAccessExpr(target, indices, expr.NullSafe, new BoundType(elementType));
    }

    private static ((Type TargetType, bool IsArray, PropertyInfo? Indexer)? Resolved, Type ElementType) TryBindMultiDimIndex(
        Type targetType, int arity, BindingContext context)
    {
        if (targetType.IsArray)
        {
            var elementType = targetType.GetElementType() ?? typeof(object);
            return ((targetType, true, null), elementType);
        }

        if (targetType == typeof(object))
            return (null, typeof(object));

        var indexer = context.RuntimeContext.TypeMetadata
            .GetProperties(targetType, BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.GetIndexParameters().Length == arity);

        if (indexer != null)
            return ((targetType, false, indexer), indexer.PropertyType);

        return (null, typeof(object));
    }
}
