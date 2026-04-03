using System.Threading.Tasks;
using Alder.Binding;
using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.ReturnStatement)]
internal static class ReturnEvaluator
{
    public static object? Evaluate(BoundReturnExpr node, EvaluationContext ctx)
    {
        var value = node.Value != null ? ctx.Evaluate(node.Value) : null;
        return ControlFlowSignal.Return(value);
    }

    public static async ValueTask<object?> EvaluateAsync(BoundReturnExpr node, EvaluationContext ctx)
    {
        var value = node.Value != null ? await ctx.EvaluateAsync(node.Value) : null;
        return ControlFlowSignal.Return(value);
    }
}
