using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal static class NamedArgumentEvaluator
{
    public static object? Evaluate(BoundNamedArgumentExpr node, EvaluationContext ctx)
    {
        return new NamedArg(node.Name, ctx.Evaluate(node.Value));
    }
}
