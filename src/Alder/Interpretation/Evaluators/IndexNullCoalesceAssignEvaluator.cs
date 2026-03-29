using Alder.Binding.BoundNodes;
using Alder.Runtime;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

internal sealed class IndexNullCoalesceAssignEvaluator : INodeEvaluator<BoundIndexNullCoalesceAssignExpr>
{
    public object? Evaluate(BoundIndexNullCoalesceAssignExpr node, EvaluationContext ctx)
    {
        var target = ctx.Evaluate(node.Target);
        target = ExecutionRuntime.EnsureIndexTargetNotNull(target);
        var index = ctx.Evaluate(node.Index);
        var currentValue = MemberAccess.GetIndex(target, index, ctx.Config, ctx.Context);
        if (currentValue != null)
            return currentValue;
        var newValue = ctx.Evaluate(node.Value);
        MemberAccess.SetIndex(target, index, newValue, ctx.Config, ctx.Context);
        return newValue;
    }
}
