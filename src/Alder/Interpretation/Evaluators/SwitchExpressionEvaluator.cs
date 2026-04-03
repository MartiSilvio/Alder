using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.SwitchExpression)]
internal static class SwitchExpressionEvaluator
{
    public static object? Evaluate(BoundSwitchExpressionExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var value = ctx.Evaluate(node.Expression, ct);

        foreach (var arm in node.Arms)
        {
            var previousContext = ctx.Context;
            ctx.Context = ctx.Context.CreateChild();

            try
            {
                if (!TypeHelpers.RequireBoolean(ctx.MatchPattern(value, arm.Pattern, ct)))
                    continue;

                if (arm.WhenGuard != null)
                {
                    var guardResult = ctx.Evaluate(arm.WhenGuard, ct);
                    if (!TypeHelpers.RequireBoolean(guardResult))
                        continue;
                }

                return ctx.Evaluate(arm.Value, ct);
            }
            finally
            {
                ctx.Context = previousContext;
            }
        }

        throw new AlderException(DiagnosticDescriptors.SwitchExpressionNonExhaustive, value ?? "null");
    }

    public static async ValueTask<object?> EvaluateAsync(BoundSwitchExpressionExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var value = await ctx.EvaluateAsync(node.Expression, ct);

        foreach (var arm in node.Arms)
        {
            var previousContext = ctx.Context;
            ctx.Context = ctx.Context.CreateChild();

            try
            {
                if (!TypeHelpers.RequireBoolean(ctx.MatchPattern(value, arm.Pattern, ct)))
                    continue;

                if (arm.WhenGuard != null)
                {
                    var guardResult = await ctx.EvaluateAsync(arm.WhenGuard, ct);
                    if (!TypeHelpers.RequireBoolean(guardResult))
                        continue;
                }

                return await ctx.EvaluateAsync(arm.Value, ct);
            }
            finally
            {
                ctx.Context = previousContext;
            }
        }

        throw new AlderException(DiagnosticDescriptors.SwitchExpressionNonExhaustive, value ?? "null");
    }
}
