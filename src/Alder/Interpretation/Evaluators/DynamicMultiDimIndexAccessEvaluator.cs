using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal sealed class DynamicMultiDimIndexAccessEvaluator : INodeEvaluator<BoundDynamicMultiDimIndexAccessExpr>
{
    public object? Evaluate(BoundDynamicMultiDimIndexAccessExpr node, EvaluationContext ctx)
    {
        var target = ctx.Evaluate(node.Target);
        if (node.NullSafe && target == null)
            return null;

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        throw new AlderException(
            DiagnosticDescriptors.BadIndexerAccess,
            TypeNameFormatter.Of(target));
    }
}
