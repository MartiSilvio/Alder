using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal static class SwitchExpressionEvaluator
{
    public static object? Evaluate(BoundSwitchExpressionExpr node, EvaluationContext ctx)
    {
        var value = ctx.Evaluate(node.Expression);

        foreach (var arm in node.Arms)
        {
            var previousContext = ctx.Context;
            ctx.Context = ctx.Context.CreateChild();

            try
            {
                if (!TypeHelpers.RequireBoolean(ctx.MatchPattern(value, arm.Pattern)))
                    continue;

                if (arm.WhenGuard != null)
                {
                    var guardResult = ctx.Evaluate(arm.WhenGuard);
                    if (!TypeHelpers.RequireBoolean(guardResult))
                        continue;
                }

                return ctx.Evaluate(arm.Value);
            }
            finally
            {
                ctx.Context = previousContext;
            }
        }

        throw new AlderException(DiagnosticDescriptors.SwitchExpressionNonExhaustive, value ?? "null");
    }
}
