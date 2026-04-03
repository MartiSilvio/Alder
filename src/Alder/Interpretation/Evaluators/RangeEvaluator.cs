using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.RangeExpression)]
internal static class RangeEvaluator
{
    public static object? Evaluate(BoundRangeExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var startValue = node.Start != null ? ctx.Evaluate(node.Start, ct) : null;
        var endValue = node.End != null ? ctx.Evaluate(node.End, ct) : null;
        var sysRange = ConstructionRuntime.CreateSystemRange(startValue, endValue);
        return node.ExclusiveEnd ? sysRange : new InclusiveRange(sysRange);
    }

    public static async ValueTask<object?> EvaluateAsync(BoundRangeExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var startValue = node.Start != null ? await ctx.EvaluateAsync(node.Start, ct) : null;
        var endValue = node.End != null ? await ctx.EvaluateAsync(node.End, ct) : null;
        var sysRange = ConstructionRuntime.CreateSystemRange(startValue, endValue);
        return node.ExclusiveEnd ? sysRange : new InclusiveRange(sysRange);
    }
}
