using System.Threading.Tasks;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.ForStatement)]
internal static class ForEvaluator
{
    public static object? Evaluate(BoundForExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var constraintState = ctx.ConstraintState;
        var constraints = ctx.Context.Config.Constraints;
        var loopContext = ctx.Context;
        ctx.Context = ctx.Context.CreateChild();
        ctx.BreakContextDepth++;
        ctx.LoopDepth++;
        try
        {
            foreach (var initializer in node.Initializers)
            {
                ctx.Evaluate(initializer, ct);
            }

            while (node.Condition == null || TypeHelpers.RequireBoolean(ctx.Evaluate(node.Condition, ct)))
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
                    if (signal.SignalKind != ControlFlowSignal.Kind.Continue) return signal;
                }

                foreach (var increment in node.Increments)
                {
                    ctx.Evaluate(increment, ct);
                }
            }
        }
        finally
        {
            ctx.LoopDepth--;
            ctx.BreakContextDepth--;
            ctx.Context = loopContext;
        }

        return null;
    }

    public static async ValueTask<object?> EvaluateAsync(BoundForExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var constraintState = ctx.ConstraintState;
        var constraints = ctx.Context.Config.Constraints;
        var loopContext = ctx.Context;
        ctx.Context = ctx.Context.CreateChild();
        ctx.BreakContextDepth++;
        ctx.LoopDepth++;
        try
        {
            foreach (var initializer in node.Initializers)
            {
                await ctx.EvaluateAsync(initializer, ct);
            }

            while (node.Condition == null || TypeHelpers.RequireBoolean(await ctx.EvaluateAsync(node.Condition, ct)))
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
                    if (signal.SignalKind != ControlFlowSignal.Kind.Continue) return signal;
                }

                foreach (var increment in node.Increments)
                {
                    await ctx.EvaluateAsync(increment, ct);
                }
            }
        }
        finally
        {
            ctx.LoopDepth--;
            ctx.BreakContextDepth--;
            ctx.Context = loopContext;
        }

        return null;
    }
}
