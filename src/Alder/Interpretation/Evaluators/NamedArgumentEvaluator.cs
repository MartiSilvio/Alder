using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.NamedArgument)]
internal static class NamedArgumentEvaluator
{
    public static object? Evaluate(BoundNamedArgumentExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        return new NamedArg(node.Name, ctx.Evaluate(node.Value, ct));
    }
}
