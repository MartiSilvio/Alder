using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.RangeExpression)]
internal static class RangeEvaluator
{
    public static object? Evaluate(BoundRangeExpr node, EvaluationContext ctx)
    {
        var startValue = node.Start != null ? ctx.Evaluate(node.Start) : null;
        var endValue = node.End != null ? ctx.Evaluate(node.End) : null;
        var sysRange = ConstructionRuntime.CreateSystemRange(startValue, endValue);
        return node.ExclusiveEnd ? sysRange : new InclusiveRange(sysRange);
    }
}
