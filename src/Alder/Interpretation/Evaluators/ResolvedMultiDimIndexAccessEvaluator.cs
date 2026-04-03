using System.Collections.Immutable;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.ResolvedMultiDimIndexAccess)]
internal static class ResolvedMultiDimIndexAccessEvaluator
{
    public static object? Evaluate(BoundResolvedMultiDimIndexAccessExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = ctx.Evaluate(node.Target, ct);
        if (node.NullSafe && target == null)
            return null;

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        if (node.IsArray)
        {
            var indices = EvaluateIntIndices(node.Indices, ctx, ct);
            return ((Array)target).GetValue(indices);
        }

        if (node.Indexer is { } indexer)
        {
            var convertedIndices = EvaluateConvertedIndices(node.Indices, indexer, ctx, ct);
            return indexer.GetValue(target, convertedIndices);
        }

        throw new AlderException(
            DiagnosticDescriptors.BadIndexerAccess,
            TypeNameFormatter.Of(target));
    }

    private static int[] EvaluateIntIndices(ImmutableArray<BoundExpr> indexExprs, EvaluationContext ctx, CancellationToken ct)
    {
        var indices = new int[indexExprs.Length];
        for (var i = 0; i < indexExprs.Length; i++)
            indices[i] = Convert.ToInt32(ctx.Evaluate(indexExprs[i], ct));
        return indices;
    }

    private static object[] EvaluateConvertedIndices(ImmutableArray<BoundExpr> indexExprs, PropertyInfo indexer, EvaluationContext ctx, CancellationToken ct)
    {
        var indexParams = indexer.GetIndexParameters();
        var converted = new object[indexExprs.Length];
        for (var i = 0; i < indexExprs.Length; i++)
        {
            var value = ctx.Evaluate(indexExprs[i], ct);
            converted[i] = Convert.ChangeType(value, indexParams[i].ParameterType);
        }
        return converted;
    }
}
