using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class DynamicIndexAccessEmitter : INodeEmitter<BoundDynamicIndexAccessExpr>
{
    public LinqExpression Emit(BoundDynamicIndexAccessExpr node, EmissionContext ctx)
    {
        var targetExpr = ctx.EmitBoxed(node.Target);
        var indexExpr = ctx.EmitBoxed(node.Index);

        if (!node.NullSafe)
        {
            return LinqExpression.Call(
                GetIndexMethod,
                targetExpr,
                indexExpr,
                ctx.ConfigParam,
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
                LinqExpression.Call(GetIndexMethod, targetVar, indexExpr, ctx.ConfigParam, ctx.ContextParam)));
    }
}
