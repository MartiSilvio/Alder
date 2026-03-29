using Alder.Binding.BoundNodes;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class DoWhileEmitter : INodeEmitter<BoundDoWhileExpr>
{
    public LinqExpression Emit(BoundDoWhileExpr node, EmissionContext ctx)
    {
        var loopBreakLabel = LinqExpression.Label(typeof(object), "doBreak");
        var loopContinueLabel = LinqExpression.Label("doContinue");
        var resultVar = LinqExpression.Variable(typeof(object), "doResult");
        var body = new List<LinqExpression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object)))
        };

        var previousDepth = ctx.LoopDepth;
        ctx.LoopDepth = previousDepth + 1;
        try
        {
            BlockEmitter.EmitLoopIterationBody(ctx, body, node.Body, resultVar, loopBreakLabel, loopContinueLabel, hasConditionCheck: false);
            body.Add(LinqExpression.Label(loopContinueLabel));
            body.Add(LinqExpression.IfThen(
                LinqExpression.Not(ctx.EmitBoolCondition(node.Condition)),
                LinqExpression.Break(loopBreakLabel, resultVar)));

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
