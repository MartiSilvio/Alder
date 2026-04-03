using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.NullCoalescingAssignmentOperator)]
internal static class NullCoalesceAssignEvaluator
{
    public static object? Evaluate(BoundNullCoalesceAssignExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var name = node.Name;
        ExecutionRuntime.CheckNullCoalesceAssignAllowed(name, ctx.Context);

        var currentValue = ctx.Context.Get(name);
        if (currentValue != null)
            return currentValue;

        var newValue = ctx.Evaluate(node.Value, ct);
        ctx.Context.Set(name, newValue);

        return newValue;
    }
}
