using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal static class OutArgEvaluator
{
    public static object? Evaluate(BoundOutArgExpr node, EvaluationContext ctx)
    {
        return new OutArgMarker(node.VariableName, node.TypeName, node.IsDiscard);
    }
}
