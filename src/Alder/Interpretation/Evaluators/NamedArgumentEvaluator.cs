using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal sealed class NamedArgumentEvaluator : INodeEvaluator<BoundNamedArgumentExpr>
{
    public object? Evaluate(BoundNamedArgumentExpr node, EvaluationContext ctx)
    {
        return new NamedArg(node.Name, ctx.Evaluate(node.Value));
    }
}
