using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

internal sealed class NullCoalesceAssignEvaluator : INodeEvaluator<BoundNullCoalesceAssignExpr>
{
    public object? Evaluate(BoundNullCoalesceAssignExpr node, EvaluationContext ctx)
    {
        var name = node.Name;
        ExecutionRuntime.CheckNullCoalesceAssignAllowed(name, ctx.Context);

        var currentValue = ctx.Context.Get(name);
        if (currentValue != null)
            return currentValue;

        var newValue = ctx.Evaluate(node.Value);
        ctx.Context.Set(name, newValue);

        return newValue;
    }
}
