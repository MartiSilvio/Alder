using Alder.Binding.BoundNodes;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class MultiDimEmitter :
    INodeEmitter<BoundMultiDimArrayInitExpr>,
    INodeEmitter<BoundResolvedMultiDimIndexAccessExpr>,
    INodeEmitter<BoundDynamicMultiDimIndexAccessExpr>,
    INodeEmitter<BoundMultiDimIndexAssignExpr>
{
    public LinqExpression Emit(BoundMultiDimArrayInitExpr node, EmissionContext ctx)
    {
        var dimensions = LinqExpression.NewArrayInit(
            typeof(int),
            node.InferredDimensions.Select(d => LinqExpression.Constant(d)));
        var flatValues = LinqExpression.NewArrayInit(
            typeof(object),
            node.FlatValues.Select(v => ctx.EmitBoxed(v)));

        return EmitHelpers.AsObject(LinqExpression.Call(
            typeof(RuntimeArrayFactory).GetMethod(nameof(RuntimeArrayFactory.CreateAndFill))!,
            LinqExpression.Constant(node.ElementType, typeof(Type)),
            dimensions,
            flatValues));
    }

    public LinqExpression Emit(BoundDynamicMultiDimIndexAccessExpr node, EmissionContext ctx)
    {
        var target = ctx.EmitBoxed(node.Target);
        var indices = LinqExpression.NewArrayInit(
            typeof(object),
            node.Indices.Select(index => ctx.EmitBoxed(index)));

        return EmitNullSafeMultiDimGet(target, indices, node.NullSafe);
    }

    public LinqExpression Emit(BoundResolvedMultiDimIndexAccessExpr node, EmissionContext ctx)
    {
        if (node.IsArray)
        {
            var getMethod = node.TargetType.GetMethod("Get");
            if (getMethod != null)
                return EmitArrayGet(node, getMethod, ctx);
        }

        if (node.Indexer is { } indexer)
            return EmitIndexerGet(node, indexer, ctx);

        var target = ctx.EmitBoxed(node.Target);
        var indices = LinqExpression.NewArrayInit(
            typeof(object),
            node.Indices.Select(index => ctx.EmitBoxed(index)));

        return EmitNullSafeMultiDimGet(target, indices, node.NullSafe);
    }

    public LinqExpression Emit(BoundMultiDimIndexAssignExpr node, EmissionContext ctx)
    {
        if (node.IsArray && node.TargetType != null)
        {
            var setMethod = node.TargetType.GetMethod("Set");
            if (setMethod != null)
                return EmitArraySet(node, setMethod, ctx);
        }

        if (node.Indexer is { CanWrite: true } indexer)
            return EmitIndexerSet(node, indexer, ctx);

        var indices = LinqExpression.NewArrayInit(
            typeof(object),
            node.Indices.Select(index => ctx.EmitBoxed(index)));
        return LinqExpression.Call(
            MultiDimArraySetMethod,
            ctx.EmitBoxed(node.Target),
            indices,
            ctx.EmitBoxed(node.Value));
    }

    private static LinqExpression EmitArrayGet(BoundResolvedMultiDimIndexAccessExpr node, MethodInfo getMethod, EmissionContext ctx)
    {
        var intIndices = node.Indices.Select(
            index => EmitHelpers.EnsureTypedExpression(ctx.Emit(index), typeof(int))).ToArray();

        if (!node.NullSafe)
        {
            var typedTarget = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Target), node.TargetType);
            return LinqExpression.Call(typedTarget, getMethod, intIndices);
        }

        var targetVar = LinqExpression.Variable(typeof(object), "mdTarget");
        return LinqExpression.Block(
            typeof(object),
            [targetVar],
            LinqExpression.Assign(targetVar, ctx.EmitBoxed(node.Target)),
            LinqExpression.Condition(
                LinqExpression.Equal(targetVar, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Constant(null, typeof(object)),
                EmitHelpers.AsObject(
                    LinqExpression.Call(
                        EmitHelpers.EnsureTypedExpression(targetVar, node.TargetType),
                        getMethod,
                        intIndices))));
    }

    private static LinqExpression EmitIndexerGet(BoundResolvedMultiDimIndexAccessExpr node, PropertyInfo indexer, EmissionContext ctx)
    {
        var indexParams = indexer.GetIndexParameters();
        var emittedIndices = new LinqExpression[node.Indices.Length];
        for (var i = 0; i < node.Indices.Length; i++)
            emittedIndices[i] = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Indices[i]), indexParams[i].ParameterType);

        var typedTarget = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Target), node.TargetType);
        return LinqExpression.Property(typedTarget, indexer, emittedIndices);
    }

    private static LinqExpression EmitArraySet(BoundMultiDimIndexAssignExpr node, MethodInfo setMethod, EmissionContext ctx)
    {
        var intIndices = node.Indices.Select(
            index => EmitHelpers.EnsureTypedExpression(ctx.Emit(index), typeof(int))).ToArray();
        var elementType = node.TargetType!.GetElementType()!;
        var typedValue = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Value), elementType);
        var typedTarget = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Target), node.TargetType!);
        var args = intIndices.Append(typedValue).ToArray();
        return LinqExpression.Block(
            typeof(object),
            LinqExpression.Call(typedTarget, setMethod, args),
            EmitHelpers.AsObject(typedValue));
    }

    private static LinqExpression EmitIndexerSet(BoundMultiDimIndexAssignExpr node, PropertyInfo indexer, EmissionContext ctx)
    {
        var indexParams = indexer.GetIndexParameters();
        var emittedIndices = new LinqExpression[node.Indices.Length];
        for (var i = 0; i < node.Indices.Length; i++)
            emittedIndices[i] = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Indices[i]), indexParams[i].ParameterType);

        var typedTarget = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Target), node.TargetType!);
        var typedValue = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Value), indexer.PropertyType);
        return LinqExpression.Block(
            typeof(object),
            LinqExpression.Assign(
                LinqExpression.Property(typedTarget, indexer, emittedIndices),
                typedValue),
            EmitHelpers.AsObject(typedValue));
    }

    private static LinqExpression EmitNullSafeMultiDimGet(LinqExpression target, LinqExpression indices, bool nullSafe)
    {
        if (!nullSafe)
            return LinqExpression.Call(MultiDimArrayGetMethod, target, indices);

        var targetVar = LinqExpression.Variable(typeof(object), "mdTarget");
        return LinqExpression.Block(
            typeof(object),
            [targetVar],
            LinqExpression.Assign(targetVar, target),
            LinqExpression.Condition(
                LinqExpression.Equal(targetVar, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Constant(null, typeof(object)),
                LinqExpression.Call(MultiDimArrayGetMethod, targetVar, indices)));
    }
}
