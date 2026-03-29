using Alder.Binding.BoundNodes;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class WhileEmitter : INodeEmitter<BoundWhileExpr>
{
    public LinqExpression Emit(BoundWhileExpr node, EmissionContext ctx)
    {
        var loopBreakLabel = LinqExpression.Label(typeof(object), "whileBreak");
        var loopContinueLabel = LinqExpression.Label("whileContinue");
        var resultVar = LinqExpression.Variable(typeof(object), "whileResult");
        var body = new List<LinqExpression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
            LinqExpression.IfThen(
                LinqExpression.Not(ctx.EmitBoolCondition(node.Condition)),
                LinqExpression.Break(loopBreakLabel, resultVar))
        };

        var previousDepth = ctx.LoopDepth;
        ctx.LoopDepth = previousDepth + 1;
        try
        {
            BlockEmitter.EmitLoopIterationBody(ctx, body, node.Body, resultVar, loopBreakLabel, loopContinueLabel, hasConditionCheck: true);
            body.Add(LinqExpression.Label(loopContinueLabel));

            return LinqExpression.Block(
                typeof(object),
                [resultVar],
                LinqExpression.Loop(LinqExpression.Block(body), loopBreakLabel),
                resultVar);
        }
        finally
        {
            ctx.LoopDepth = previousDepth;
        }
    }
}
