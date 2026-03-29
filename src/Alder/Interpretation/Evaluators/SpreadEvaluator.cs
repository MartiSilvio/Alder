using Alder.Binding.BoundNodes;
using Alder.Diagnostics;

namespace Alder.Interpretation.Evaluators;

internal sealed class SpreadEvaluator : INodeEvaluator<BoundSpreadExpr>
{
    public object? Evaluate(BoundSpreadExpr node, EvaluationContext ctx)
    {
        throw new AlderException(DiagnosticDescriptors.SpreadOutsideLiteral);
    }
}
