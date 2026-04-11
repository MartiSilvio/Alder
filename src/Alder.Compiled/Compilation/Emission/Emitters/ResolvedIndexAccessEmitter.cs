using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.ResolvedIndexAccess)]
internal static class ResolvedIndexAccessEmitter
{
    public static LinqExpression Emit(BoundResolvedIndexAccessExpr node, EmissionContext ctx)
    {
        if (node.IsDirectCollectionAccess)
            return EmitDirectCollectionAccess(node, ctx);

        var targetExpr = ctx.EmitBoxed(node.Target);
        var indexExpr = ctx.EmitBoxed(node.Index);

        if (!node.NullSafe)
        {
            return LinqExpression.Call(
                GetIndexMethod,
                targetExpr,
                indexExpr,
                ctx.ContextParam);
        }

        var targetVar = LinqExpression.Variable(typeof(object), "indexTarget");
        return LinqExpression.Block(
            typeof(object),
            [targetVar],
            LinqExpression.Assign(targetVar, targetExpr),
            LinqExpression.Condition(
                LinqExpression.Equal(targetVar, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Constant(null, typeof(object)),
                LinqExpression.Call(GetIndexMethod, targetVar, indexExpr, ctx.ContextParam)));
    }

    private static LinqExpression EmitDirectCollectionAccess(BoundResolvedIndexAccessExpr node, EmissionContext ctx)
    {
        if (node.TargetType == typeof(string))
            return EmitDirectStringAccess(node, ctx);

        if (typeof(IList).IsAssignableFrom(node.TargetType))
            return EmitDirectListAccess(node, ctx);

        return LinqExpression.Call(
            GetIndexMethod,
            ctx.EmitBoxed(node.Target),
            ctx.EmitBoxed(node.Index),
            ctx.ContextParam);
    }

    private static LinqExpression EmitDirectStringAccess(BoundResolvedIndexAccessExpr node, EmissionContext ctx)
    {
        var targetObjVar = LinqExpression.Variable(typeof(object), "indexTarget");
        var typedTarget = EmitHelpers.EnsureTypedExpression(
            LinqExpression.Call(EnsureIndexTargetNotNullMethod, targetObjVar),
            typeof(string));
        var indexExpr = BuildNormalizedIntIndex(node, LinqExpression.Property(typedTarget, StringLengthProperty), ctx);
        var charExpr = LinqExpression.Property(typedTarget, StringCharsProperty, indexExpr);

        if (node.NullSafe)
        {
            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                LinqExpression.Assign(targetObjVar, ctx.EmitBoxed(node.Target)),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    LinqExpression.Convert(charExpr, typeof(object))));
        }

        return LinqExpression.Block(
            typeof(char),
            [targetObjVar],
            LinqExpression.Assign(targetObjVar, ctx.EmitBoxed(node.Target)),
            charExpr);
    }

    private static LinqExpression EmitDirectListAccess(BoundResolvedIndexAccessExpr node, EmissionContext ctx)
    {
        var targetObjVar = LinqExpression.Variable(typeof(object), "listTarget");
        var checkedTarget = LinqExpression.Call(EnsureIndexTargetNotNullMethod, targetObjVar);

        LinqExpression typedTarget;
        LinqExpression countExpr;
        LinqExpression valueExpr;
        Type valueType;

        if (EmitHelpers.TryGetIntIndexer(node.TargetType, out var indexer) &&
            EmitHelpers.TryGetCountProperty(node.TargetType, out var countProperty))
        {
            typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, node.TargetType);
            countExpr = LinqExpression.Property(typedTarget, countProperty);
            var indexExpr = BuildNormalizedIntIndex(node, countExpr, ctx);
            valueExpr = LinqExpression.Property(typedTarget, indexer, indexExpr);
            valueType = indexer.PropertyType;
        }
        else
        {
            typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, typeof(IList));
            countExpr = LinqExpression.Property(
                EmitHelpers.EnsureTypedExpression(typedTarget, typeof(ICollection)),
                ICollectionCountProperty);
            var indexExpr = BuildNormalizedIntIndex(node, countExpr, ctx);
            valueExpr = LinqExpression.Property(typedTarget, IListIndexerProperty, indexExpr);
            valueType = typeof(object);
        }

        var guardedValueExpr = EmitHelpers.WrapGuardedValue(valueExpr, valueType, "index access");

        if (node.NullSafe)
        {
            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                LinqExpression.Assign(targetObjVar, ctx.EmitBoxed(node.Target)),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    EmitHelpers.AsObject(guardedValueExpr)));
        }

        return LinqExpression.Block(
            valueType,
            [targetObjVar],
            LinqExpression.Assign(targetObjVar, ctx.EmitBoxed(node.Target)),
            guardedValueExpr);
    }

    private static LinqExpression BuildNormalizedIntIndex(BoundResolvedIndexAccessExpr node, LinqExpression lengthExpression, EmissionContext ctx)
    {
        if (node.Index is BoundLiteralExpr { Value: int literalIndex and >= 0 })
            return LinqExpression.Constant(literalIndex, typeof(int));

        var rawIndex = LinqExpression.Call(ConvertToInt32ObjectMethod, ctx.EmitBoxed(node.Index));
        return LinqExpression.Call(NormalizeIndexMethod, rawIndex, lengthExpression);
    }
}
