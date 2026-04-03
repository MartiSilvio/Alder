using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class MemberNullCoalesceAssignEmitter : INodeEmitter<BoundMemberNullCoalesceAssignExpr>
{
    public LinqExpression Emit(BoundMemberNullCoalesceAssignExpr node, EmissionContext ctx)
    {
        var targetVar = LinqExpression.Variable(typeof(object), "nca_target");
        var currentVar = LinqExpression.Variable(typeof(object), "nca_current");
        var resultVar = LinqExpression.Variable(typeof(object), "nca_result");
        var memberName = LinqExpression.Constant(node.MemberName);

        return LinqExpression.Block(
            typeof(object),
            [targetVar, currentVar, resultVar],
            LinqExpression.Assign(targetVar, LinqExpression.Call(
                EnsureMemberTargetNotNullMethod,
                ctx.EmitBoxed(node.Target),
                memberName)),
            LinqExpression.Assign(currentVar, LinqExpression.Call(
                GetMemberMethod, targetVar, memberName,
                LinqExpression.Constant(false), ctx.ContextParam)),
            LinqExpression.IfThenElse(
                LinqExpression.NotEqual(currentVar, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Assign(resultVar, currentVar),
                LinqExpression.Block(
                    LinqExpression.Assign(resultVar, ctx.EmitBoxed(node.Value)),
                    LinqExpression.Call(SetMemberMethod, targetVar, memberName, resultVar, ctx.ContextParam))),
            resultVar);
    }
}
