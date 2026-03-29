using Alder.Binding.BoundNodes;
using Alder.Runtime;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

internal sealed class ForEvaluator : INodeEvaluator<BoundForExpr>
{
    public object? Evaluate(BoundForExpr node, EvaluationContext ctx)
    {
        var constraintState = ctx.ConstraintState;
        var constraints = ctx.Config.Constraints;
        var loopContext = ctx.Context;
        ctx.Context = ctx.Context.CreateChild();
        var bodyContext = ctx.Context.CreateChild();

        ctx.BreakContextDepth++;
        ctx.LoopDepth++;
        try
        {
            foreach (var initializer in node.Initializers)
            {
                ctx.Evaluate(initializer);
            }

            while (node.Condition == null || TypeHelpers.RequireBoolean(ctx.Evaluate(node.Condition)))
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, ctx.CancellationToken);
                ExecutionRuntime.CheckLoopIterationConstraint(constraintState, constraints);
                bodyContext.ClearScope();

                var previousContext = ctx.Context;
                ctx.Context = bodyContext;

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
                    if (signal.SignalKind != ControlFlowSignal.Kind.Continue) return signal;
                }

                foreach (var increment in node.Increments)
                {
                    ctx.Evaluate(increment);
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
