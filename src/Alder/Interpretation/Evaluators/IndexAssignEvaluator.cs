using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

internal static class IndexAssignEvaluator
{
    public static object? Evaluate(BoundIndexAssignExpr node, EvaluationContext ctx)
    {
        var target = ctx.Evaluate(node.Target);
        var index = ctx.Evaluate(node.Index);
        var value = ctx.Evaluate(node.Value);
        return AssignmentRuntime.ApplyIndexAssign(target, index, value, ctx.Config, ctx.Context);
    }
}
