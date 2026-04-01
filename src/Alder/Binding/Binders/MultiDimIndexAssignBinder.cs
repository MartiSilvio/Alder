using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(MultiDimIndexAssignExpr))]
internal static class MultiDimIndexAssignBinder
{
    public static BoundExpr Bind(MultiDimIndexAssignExpr expr, BindingContext context, BinderContext binder)
    {
        var target = binder.Bind(expr.Object, context);
        var indices = expr.Indices
            .Select(index => binder.Bind(index, context))
            .ToImmutableArray();
        var value = binder.Bind(expr.Value, context);
        var (resolved, _) = TryBindMultiDimIndex(target.StaticType.ClrType, indices.Length, context);
        return new BoundMultiDimIndexAssignExpr(target, indices, value,
            resolved?.TargetType, resolved?.IsArray ?? false, resolved?.Indexer, value.StaticType);
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
