using Alder.Binding;
using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.GotoCaseStatement)]
internal static class GotoCaseEvaluator
{
    public static object? Evaluate(BoundGotoCaseExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        return ControlFlowSignal.GotoCaseSignal(ctx.Evaluate(node.Value, ct));
    }

    public static async ValueTask<object?> EvaluateAsync(BoundGotoCaseExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        return ControlFlowSignal.GotoCaseSignal(await ctx.EvaluateAsync(node.Value, ct));
    }
}
