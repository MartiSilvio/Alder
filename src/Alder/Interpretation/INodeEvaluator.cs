using Alder.Binding;

namespace Alder.Interpretation;

internal interface INodeEvaluator<in TNode> where TNode : BoundExpr
{
    object? Evaluate(TNode node, EvaluationContext ctx);
}
