using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.ThrowExpression)]
internal static class ThrowEvaluator
{
    public static object? Evaluate(BoundThrowExpr node, EvaluationContext ctx)
    {
        if (node.Expression != null)
        {
            var result = ctx.Evaluate(node.Expression);
            throw ExecutionRuntime.ValidateThrowOperand(result);
        }

        if (ctx.CaughtExceptions.Count == 0)
            throw new AlderException(DiagnosticDescriptors.ThrowOutsideCatch);

        ExceptionDispatchInfo.Capture(ctx.CaughtExceptions.Peek()).Throw();
        return null;
    }

    public static async ValueTask<object?> EvaluateAsync(BoundThrowExpr node, EvaluationContext ctx)
    {
        if (node.Expression != null)
        {
            var result = await ctx.EvaluateAsync(node.Expression);
            throw ExecutionRuntime.ValidateThrowOperand(result);
        }

        if (ctx.CaughtExceptions.Count == 0)
            throw new AlderException(DiagnosticDescriptors.ThrowOutsideCatch);

        ExceptionDispatchInfo.Capture(ctx.CaughtExceptions.Peek()).Throw();
        return null;
    }
}
