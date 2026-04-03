using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class IndexNullCoalesceAssignEmitter : INodeEmitter<BoundIndexNullCoalesceAssignExpr>
{
    public LinqExpression Emit(BoundIndexNullCoalesceAssignExpr node, EmissionContext ctx)
    {
        var targetVar = LinqExpression.Variable(typeof(object), "nca_target");
        var indexVar = LinqExpression.Variable(typeof(object), "nca_index");
        var currentVar = LinqExpression.Variable(typeof(object), "nca_current");
        var resultVar = LinqExpression.Variable(typeof(object), "nca_result");

        return LinqExpression.Block(
            typeof(object),
            [targetVar, indexVar, currentVar, resultVar],
            LinqExpression.Assign(targetVar, LinqExpression.Call(
                EnsureIndexTargetNotNullMethod,
                ctx.EmitBoxed(node.Target))),
            LinqExpression.Assign(indexVar, ctx.EmitBoxed(node.Index)),
            LinqExpression.Assign(currentVar, LinqExpression.Call(
                GetIndexMethod, targetVar, indexVar, ctx.ContextParam)),
            LinqExpression.IfThenElse(
                LinqExpression.NotEqual(currentVar, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Assign(resultVar, currentVar),
                LinqExpression.Block(
                    LinqExpression.Assign(resultVar, ctx.EmitBoxed(node.Value)),
                    LinqExpression.Call(SetIndexMethod, targetVar, indexVar, resultVar, ctx.ContextParam))),
            resultVar);
    }
}
