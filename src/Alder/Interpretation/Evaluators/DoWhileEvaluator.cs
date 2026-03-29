using Alder.Binding.BoundNodes;
using Alder.Runtime;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

internal static class DoWhileEvaluator
{
    public static object? Evaluate(BoundDoWhileExpr node, EvaluationContext ctx)
    {
        var constraintState = ctx.ConstraintState;
        var constraints = ctx.Config.Constraints;
        var iterationContext = ctx.Context.CreateChild();

        ctx.BreakContextDepth++;
        ctx.LoopDepth++;
        try
        {
            do
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, ctx.CancellationToken);
                ExecutionRuntime.CheckLoopIterationConstraint(constraintState, constraints);
                iterationContext.ClearScope();

                var previousContext = ctx.Context;
                ctx.Context = iterationContext;

                ControlFlowSignal? signal;
                try
                {
                    signal = BlockEvaluator.ExecuteStatementBlock(node.Body, ctx);
                }
                finally
                {
                    ctx.Context = previousContext;
                }

                if (signal != null)
                {
                    if (signal.SignalKind == ControlFlowSignal.Kind.Break) break;
                    if (signal.SignalKind == ControlFlowSignal.Kind.Continue) continue;
                    return signal;
                }
            } while (TypeHelpers.RequireBoolean(ctx.Evaluate(node.Condition)));

            return null;
        }
        finally
        {
            ctx.LoopDepth--;
            ctx.BreakContextDepth--;
        }
    }
}
