using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal sealed class OutArgEvaluator : INodeEvaluator<BoundOutArgExpr>
{
    public object? Evaluate(BoundOutArgExpr node, EvaluationContext ctx)
    {
        return new OutArgMarker(node.VariableName, node.TypeName, node.IsDiscard);
    }
}
