using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.ForStatement)]
internal static class ForEmitter
{
    public static LinqExpression Emit(BoundForExpr node, EmissionContext ctx)
    {
        var previousContextVar = LinqExpression.Variable(typeof(AlderContext), "forPrevCtx");
        var resultVar = LinqExpression.Variable(typeof(object), "forResult");
        var loopBreakLabel = LinqExpression.Label(typeof(object), "forBreak");
        var loopContinueLabel = LinqExpression.Label("forContinue");

        var prologue = new List<LinqExpression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
            LinqExpression.Assign(previousContextVar, ctx.ContextParam),
            LinqExpression.Assign(ctx.ContextParam, LinqExpression.Call(ctx.ContextParam, ContextCreateChildMethod))
        };

        var previousDepth = ctx.LoopDepth;
        ctx.LoopDepth = previousDepth + 1;
        try
        {
            for (var i = 0; i < node.Initializers.Length; i++)
                prologue.Add(ctx.Emit(node.Initializers[i]));

            var body = new List<LinqExpression>();
            if (node.Condition != null)
            {
                body.Add(LinqExpression.IfThen(
                    LinqExpression.Not(ctx.EmitBoolCondition(node.Condition)),
                    LinqExpression.Break(loopBreakLabel, resultVar)));
            }

            BlockEmitter.EmitLoopIterationBody(ctx, body, node.Body, resultVar, loopBreakLabel, loopContinueLabel, hasConditionCheck: false);
            body.Add(LinqExpression.Label(loopContinueLabel));
            for (var i = 0; i < node.Increments.Length; i++)
                body.Add(ctx.Emit(node.Increments[i]));

            return LinqExpression.Block(
                typeof(object),
                [previousContextVar, resultVar],
                LinqExpression.TryFinally(
                    LinqExpression.Block(
                        prologue.Append(
                            LinqExpression.Loop(LinqExpression.Block(body), loopBreakLabel))),
                    LinqExpression.Assign(ctx.ContextParam, previousContextVar)),
                resultVar);
        }
        finally
        {
            ctx.LoopDepth = previousDepth;
        }
    }
}
