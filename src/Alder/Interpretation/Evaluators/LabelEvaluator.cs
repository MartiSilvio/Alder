using Alder.Binding;
using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.Label)]
internal static class LabelEvaluator
{
    public static object? Evaluate(BoundLabelExpr node, EvaluationContext ctx)
    {
        return null;
    }
}
