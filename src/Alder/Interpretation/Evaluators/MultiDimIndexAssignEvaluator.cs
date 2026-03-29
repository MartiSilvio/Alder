using System.Collections.Immutable;
using System.Reflection;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal static class MultiDimIndexAssignEvaluator
{
    public static object? Evaluate(BoundMultiDimIndexAssignExpr node, EvaluationContext ctx)
    {
        var target = ctx.Evaluate(node.Target);

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        var value = ctx.Evaluate(node.Value);

        if (node.IsArray)
        {
            var indices = EvaluateIntIndices(node.Indices, ctx);
            ((Array)target).SetValue(value, indices);
            return value;
        }

        if (node.Indexer is { CanWrite: true } indexer)
        {
            var convertedIndices = EvaluateConvertedIndices(node.Indices, indexer, ctx);
            indexer.SetValue(target, value, convertedIndices);
            return value;
        }

        throw new AlderException(
            DiagnosticDescriptors.BadIndexerAccess,
            TypeNameFormatter.Of(target));
    }

    private static int[] EvaluateIntIndices(ImmutableArray<BoundExpr> indexExprs, EvaluationContext ctx)
    {
        var indices = new int[indexExprs.Length];
        for (var i = 0; i < indexExprs.Length; i++)
            indices[i] = Convert.ToInt32(ctx.Evaluate(indexExprs[i]));
        return indices;
    }

    private static object[] EvaluateConvertedIndices(ImmutableArray<BoundExpr> indexExprs, PropertyInfo indexer, EvaluationContext ctx)
    {
        var indexParams = indexer.GetIndexParameters();
        var converted = new object[indexExprs.Length];
        for (var i = 0; i < indexExprs.Length; i++)
        {
            var value = ctx.Evaluate(indexExprs[i]);
            converted[i] = Convert.ChangeType(value, indexParams[i].ParameterType);
        }
        return converted;
    }
}
