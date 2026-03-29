using System.Linq.Expressions;
using System.Reflection;
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
    public Expression Emit(BoundMultiDimArrayInitExpr node, EmissionContext ctx)
    {
        var dimensions = LinqExpression.NewArrayInit(
            typeof(int),
            node.InferredDimensions.Select(d => LinqExpression.Constant(d)));
        var flatValues = LinqExpression.NewArrayInit(
            typeof(object),
            node.FlatValues.Select(v => EmitHelpers.AsObject(ctx.Emit(v))));

        return EmitHelpers.AsObject(LinqExpression.Call(
            typeof(RuntimeArrayFactory).GetMethod(nameof(RuntimeArrayFactory.CreateAndFill))!,
            LinqExpression.Constant(node.ElementType, typeof(Type)),
            dimensions,
            flatValues));
    }

    public Expression Emit(BoundDynamicMultiDimIndexAccessExpr node, EmissionContext ctx)
    {
        var target = EmitHelpers.AsObject(ctx.Emit(node.Target));
        var indices = LinqExpression.NewArrayInit(
            typeof(object),
            node.Indices.Select(index => EmitHelpers.AsObject(ctx.Emit(index))));

        return EmitNullSafeMultiDimGet(target, indices, node.NullSafe);
    }

    public Expression Emit(BoundResolvedMultiDimIndexAccessExpr node, EmissionContext ctx)
    {
        if (node.IsArray)
        {
            var getMethod = node.TargetType.GetMethod("Get");
            if (getMethod != null)
                return EmitArrayGet(node, getMethod, ctx);
        }

        if (node.Indexer is { } indexer)
            return EmitIndexerGet(node, indexer, ctx);

        var target = EmitHelpers.AsObject(ctx.Emit(node.Target));
        var indices = LinqExpression.NewArrayInit(
            typeof(object),
            node.Indices.Select(index => EmitHelpers.AsObject(ctx.Emit(index))));

        return EmitNullSafeMultiDimGet(target, indices, node.NullSafe);
    }

    public Expression Emit(BoundMultiDimIndexAssignExpr node, EmissionContext ctx)
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
            node.Indices.Select(index => EmitHelpers.AsObject(ctx.Emit(index))));
        return LinqExpression.Call(
            MultiDimArraySetMethod,
            EmitHelpers.AsObject(ctx.Emit(node.Target)),
            indices,
            EmitHelpers.AsObject(ctx.Emit(node.Value)));
    }

    private static Expression EmitArrayGet(BoundResolvedMultiDimIndexAccessExpr node, MethodInfo getMethod, EmissionContext ctx)
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
            LinqExpression.Assign(targetVar, EmitHelpers.AsObject(ctx.Emit(node.Target))),
            LinqExpression.Condition(
                LinqExpression.Equal(targetVar, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Constant(null, typeof(object)),
                EmitHelpers.AsObject(
                    LinqExpression.Call(
                        EmitHelpers.EnsureTypedExpression(targetVar, node.TargetType),
                        getMethod,
                        intIndices))));
    }

    private static Expression EmitIndexerGet(BoundResolvedMultiDimIndexAccessExpr node, PropertyInfo indexer, EmissionContext ctx)
    {
        var indexParams = indexer.GetIndexParameters();
        var emittedIndices = new Expression[node.Indices.Length];
        for (var i = 0; i < node.Indices.Length; i++)
            emittedIndices[i] = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Indices[i]), indexParams[i].ParameterType);

        var typedTarget = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Target), node.TargetType);
        return LinqExpression.Property(typedTarget, indexer, emittedIndices);
    }

    private static Expression EmitArraySet(BoundMultiDimIndexAssignExpr node, MethodInfo setMethod, EmissionContext ctx)
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

    private static Expression EmitIndexerSet(BoundMultiDimIndexAssignExpr node, PropertyInfo indexer, EmissionContext ctx)
    {
        var indexParams = indexer.GetIndexParameters();
        var emittedIndices = new Expression[node.Indices.Length];
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

    private static Expression EmitNullSafeMultiDimGet(Expression target, Expression indices, bool nullSafe)
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
