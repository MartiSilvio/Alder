using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal static class SliceEvaluator
{
    public static object? Evaluate(BoundSliceExpr node, EvaluationContext ctx)
    {
        var target = ctx.Evaluate(node.Target);
        var start = node.Start != null ? ctx.Evaluate(node.Start) : null;
        var end = node.End != null ? ctx.Evaluate(node.End) : null;
        var step = node.Step != null ? ctx.Evaluate(node.Step) : null;
        return MemberAccess.GetSlice(target, start, end, step);
    }
}
