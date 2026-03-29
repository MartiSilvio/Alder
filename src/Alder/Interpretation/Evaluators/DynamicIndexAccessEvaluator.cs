using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal sealed class DynamicIndexAccessEvaluator : INodeEvaluator<BoundDynamicIndexAccessExpr>
{
    public object? Evaluate(BoundDynamicIndexAccessExpr node, EvaluationContext ctx)
    {
        var target = ctx.Evaluate(node.Target);
        if (node.NullSafe && target == null)
            return null;

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        var index = ctx.Evaluate(node.Index);
        return MemberAccess.GetIndex(target, index, ctx.Config, ctx.Context);
    }
}
