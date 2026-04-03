using System.Collections.Immutable;
using System.Threading.Tasks;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.Block)]
internal static class BlockEvaluator
{
    public static object? Evaluate(BoundBlockExpr node, EvaluationContext ctx)
    {
        var constraintState = ctx.ConstraintState;
        var constraints = ctx.Config.Constraints;
        var previousContext = ctx.Context;
        ctx.Context = ctx.Context.CreateChild();

        try
        {
            var startIndex = 0;
            ExecuteBlock:
            for (var i = startIndex; i < node.Statements.Length; i++)
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, ctx.CancellationToken);
                var result = ctx.Evaluate(node.Statements[i]);
                if (result is ControlFlowSignal signal)
                {
                    if (signal.SignalKind == ControlFlowSignal.Kind.Return)
                        return signal;
                    if (signal.SignalKind == ControlFlowSignal.Kind.Goto)
                    {
                        var labelName = (string)signal.Value!;
                        var labelIndex = FindLabelIndex(node.Statements, labelName);
                        if (labelIndex >= 0)
                        {
                            startIndex = labelIndex + 1;
                            goto ExecuteBlock;
                        }
                    }
                    return result;
                }
            }

            return node.ReturnExpr != null ? ctx.Evaluate(node.ReturnExpr) : null;
        }
        finally
        {
            ctx.Context = previousContext;
        }
    }

    internal static ControlFlowSignal? ExecuteStatementBlock(IEnumerable<BoundExpr> statements, EvaluationContext ctx)
    {
        foreach (var statement in statements)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();
            var result = ctx.Evaluate(statement);
            if (result is ControlFlowSignal signal)
                return signal;
        }

        return null;
    }

    public static async ValueTask<object?> EvaluateAsync(BoundBlockExpr node, EvaluationContext ctx)
    {
        var constraintState = ctx.ConstraintState;
        var constraints = ctx.Config.Constraints;
        var previousContext = ctx.Context;
        ctx.Context = ctx.Context.CreateChild();

        try
        {
            var startIndex = 0;
            while (true)
            {
                var restarted = false;
                for (var i = startIndex; i < node.Statements.Length; i++)
                {
                    ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, ctx.CancellationToken);
                    var result = await ctx.EvaluateAsync(node.Statements[i]);
                    if (result is ControlFlowSignal signal)
                    {
                        if (signal.SignalKind == ControlFlowSignal.Kind.Return)
                            return signal;
                        if (signal.SignalKind == ControlFlowSignal.Kind.Goto)
                        {
                            var labelName = (string)signal.Value!;
                            var labelIndex = FindLabelIndex(node.Statements, labelName);
                            if (labelIndex >= 0)
                            {
                                startIndex = labelIndex + 1;
                                restarted = true;
                                break;
                            }
                        }
                        return result;
                    }
                }
                if (!restarted) break;
            }

            return node.ReturnExpr != null ? await ctx.EvaluateAsync(node.ReturnExpr) : null;
        }
        finally
        {
            ctx.Context = previousContext;
        }
    }

    internal static async ValueTask<ControlFlowSignal?> ExecuteStatementBlockAsync(IEnumerable<BoundExpr> statements, EvaluationContext ctx)
    {
        foreach (var statement in statements)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();
            var result = await ctx.EvaluateAsync(statement);
            if (result is ControlFlowSignal signal)
                return signal;
        }

        return null;
    }

    private static int FindLabelIndex(ImmutableArray<BoundExpr> statements, string label)
    {
        for (var i = 0; i < statements.Length; i++)
            if (statements[i] is BoundLabelExpr l && l.Name == label)
                return i;
        return -1;
    }
}
