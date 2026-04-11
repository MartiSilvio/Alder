using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.DynamicIndexAccess)]
internal static class DynamicIndexAccessEmitter
{
    public static LinqExpression Emit(BoundDynamicIndexAccessExpr node, EmissionContext ctx)
    {
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
}
