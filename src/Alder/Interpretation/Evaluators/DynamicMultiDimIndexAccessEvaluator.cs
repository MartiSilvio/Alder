using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.DynamicMultiDimIndexAccess)]
internal static class DynamicMultiDimIndexAccessEvaluator
{
    public static object? Evaluate(BoundDynamicMultiDimIndexAccessExpr node, EvaluationContext ctx)
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
