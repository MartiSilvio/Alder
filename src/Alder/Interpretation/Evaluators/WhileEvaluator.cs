using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.WhileStatement)]
internal static class WhileEvaluator
{
    public static object? Evaluate(BoundWhileExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var constraintState = ctx.ConstraintState;
        var constraints = ctx.Context.Config.Constraints;
        ctx.BreakContextDepth++;
        ctx.LoopDepth++;
        try
        {
            while (TypeHelpers.RequireBoolean(ctx.Evaluate(node.Condition, ct)))
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, ct);
                ExecutionRuntime.CheckLoopIterationConstraint(constraintState, constraints);

                var previousContext = ctx.Context;
                ctx.Context = ctx.Context.CreateChild();

                ControlFlowSignal? signal;
                try
                {
                    signal = BlockEvaluator.ExecuteStatementBlock(node.Body, ctx, ct);
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
            }

            return null;
        }
        finally
        {
            ctx.LoopDepth--;
            ctx.BreakContextDepth--;
        }
    }

    public static async ValueTask<object?> EvaluateAsync(BoundWhileExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var constraintState = ctx.ConstraintState;
        var constraints = ctx.Context.Config.Constraints;
        ctx.BreakContextDepth++;
        ctx.LoopDepth++;
        try
        {
            while (TypeHelpers.RequireBoolean(await ctx.EvaluateAsync(node.Condition, ct)))
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, ct);
                ExecutionRuntime.CheckLoopIterationConstraint(constraintState, constraints);

                var previousContext = ctx.Context;
                ctx.Context = ctx.Context.CreateChild();

                ControlFlowSignal? signal;
                try
                {
                    signal = await BlockEvaluator.ExecuteStatementBlockAsync(node.Body, ctx, ct);
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
            }

            return null;
        }
        finally
        {
            ctx.LoopDepth--;
            ctx.BreakContextDepth--;
        }
    }
}
