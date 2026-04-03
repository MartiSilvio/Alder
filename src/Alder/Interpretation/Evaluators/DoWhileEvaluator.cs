using System.Threading.Tasks;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.DoStatement)]
internal static class DoWhileEvaluator
{
    public static object? Evaluate(BoundDoWhileExpr node, EvaluationContext ctx)
    {
        var constraintState = ctx.ConstraintState;
        var constraints = ctx.Config.Constraints;
        ctx.BreakContextDepth++;
        ctx.LoopDepth++;
        try
        {
            do
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, ctx.CancellationToken);
                ExecutionRuntime.CheckLoopIterationConstraint(constraintState, constraints);

                var signal = ExecuteBodyIteration(node, ctx);

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

    public static async ValueTask<object?> EvaluateAsync(BoundDoWhileExpr node, EvaluationContext ctx)
    {
        var constraintState = ctx.ConstraintState;
        var constraints = ctx.Config.Constraints;
        ctx.BreakContextDepth++;
        ctx.LoopDepth++;
        try
        {
            do
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, ctx.CancellationToken);
                ExecutionRuntime.CheckLoopIterationConstraint(constraintState, constraints);

                var signal = await ExecuteBodyIterationAsync(node, ctx);

                if (signal != null)
                {
                    if (signal.SignalKind == ControlFlowSignal.Kind.Break) break;
                    if (signal.SignalKind == ControlFlowSignal.Kind.Continue) continue;
                    return signal;
                }
            } while (TypeHelpers.RequireBoolean(await ctx.EvaluateAsync(node.Condition)));

            return null;
        }
        finally
        {
            ctx.LoopDepth--;
            ctx.BreakContextDepth--;
        }
    }

    private static ControlFlowSignal? ExecuteBodyIteration(BoundDoWhileExpr node, EvaluationContext ctx)
    {
        var previousContext = ctx.Context;
        ctx.Context = ctx.Context.CreateChild();
        try
        {
            return BlockEvaluator.ExecuteStatementBlock(node.Body, ctx);
        }
        finally
        {
            ctx.Context = previousContext;
        }
    }

    private static async ValueTask<ControlFlowSignal?> ExecuteBodyIterationAsync(BoundDoWhileExpr node, EvaluationContext ctx)
    {
        var previousContext = ctx.Context;
        ctx.Context = ctx.Context.CreateChild();
        try
        {
            return await BlockEvaluator.ExecuteStatementBlockAsync(node.Body, ctx);
        }
        finally
        {
            ctx.Context = previousContext;
        }
    }
}
