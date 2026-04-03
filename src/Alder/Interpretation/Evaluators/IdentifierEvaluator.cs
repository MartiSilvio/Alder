using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.Identifier)]
internal static class IdentifierEvaluator
{
    public static object? Evaluate(BoundIdentifierExpr node, EvaluationContext ctx)
    {
        if (node.LocalId != null)
            return ctx.Context.Get(node.Name);

        return IdentifierRuntime.ResolveIdentifier(node.Name, ctx.Context);
    }
}
