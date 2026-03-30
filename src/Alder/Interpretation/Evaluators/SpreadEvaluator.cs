using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.SpreadElement)]
internal static class SpreadEvaluator
{
    public static object? Evaluate(BoundSpreadExpr node, EvaluationContext ctx)
    {
        throw new AlderException(DiagnosticDescriptors.SpreadOutsideLiteral);
    }
}
