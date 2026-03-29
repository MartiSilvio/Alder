using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

internal sealed class IdentifierEvaluator : INodeEvaluator<BoundIdentifierExpr>
{
    public object? Evaluate(BoundIdentifierExpr node, EvaluationContext ctx)
    {
        if (node.LocalId != null)
            return ctx.Context.Get(node.Name);

        return IdentifierRuntime.ResolveIdentifier(node.Name, ctx.Context, ctx.Config);
    }
}
