using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.IfStatement)]
internal static class IfEvaluator
{
    public static object? Evaluate(BoundIfStatementExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var condition = ctx.Evaluate(node.Condition, ct);
        if (TypeHelpers.RequireBoolean(condition))
        {
            return EvaluateBranch(node.ThenStatements, ctx, ct);
        }

        if (!node.ElseStatements.IsDefaultOrEmpty)
        {
            return EvaluateBranch(node.ElseStatements, ctx, ct);
        }

        return null;
    }

    public static async ValueTask<object?> EvaluateAsync(BoundIfStatementExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var condition = await ctx.EvaluateAsync(node.Condition, ct);
        if (TypeHelpers.RequireBoolean(condition))
            return await EvaluateBranchAsync(node.ThenStatements, ctx, ct);

        if (!node.ElseStatements.IsDefaultOrEmpty)
            return await EvaluateBranchAsync(node.ElseStatements, ctx, ct);

        return null;
    }

    internal static object? EvaluateBranch(IEnumerable<BoundExpr> statements, EvaluationContext ctx, CancellationToken ct)
    {
        var previousContext = ctx.Context;
        ctx.Context = ctx.Context.CreateChild();

        try
        {
            foreach (var statement in statements)
            {
                ct.ThrowIfCancellationRequested();
                var result = ctx.Evaluate(statement, ct);
                if (result is ControlFlowSignal)
                    return result;
            }

            return null;
        }
        finally
        {
            ctx.Context = previousContext;
        }
    }

    internal static async ValueTask<object?> EvaluateBranchAsync(IEnumerable<BoundExpr> statements, EvaluationContext ctx, CancellationToken ct)
    {
        var previousContext = ctx.Context;
        ctx.Context = ctx.Context.CreateChild();

        try
        {
            foreach (var statement in statements)
            {
                ct.ThrowIfCancellationRequested();
                var result = await ctx.EvaluateAsync(statement, ct);
                if (result is ControlFlowSignal)
                    return result;
            }

            return null;
        }
        finally
        {
            ctx.Context = previousContext;
        }
    }
}
