using System.Collections;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime;
using Alder.Runtime.Extensions;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

internal static class ForEachEvaluator
{
    public static object? Evaluate(BoundForEachExpr node, EvaluationContext ctx)
    {
        var constraintState = ctx.ConstraintState;
        var constraints = ctx.Config.Constraints;
        var collection = ctx.Evaluate(node.Collection);

        collection = RangeHelpers.EnsureEnumerable(collection!);

        if (collection is not IEnumerable enumerable)
        {
            throw new AlderException(DiagnosticDescriptors.ForeachRequiresIEnumerable, TypeNameFormatter.Of(collection));
        }

        ctx.BreakContextDepth++;
        ctx.LoopDepth++;
        try
        {
            foreach (var item in enumerable)
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, ctx.CancellationToken);
                ExecutionRuntime.CheckLoopIterationConstraint(constraintState, constraints);

                var previousContext = ctx.Context;
                ctx.Context = ctx.Context.CreateChild();

                ControlFlowSignal? signal;
                try
                {
                    ctx.Context.DefineNew(node.VariableName, item, node.ElementType);
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
