using System.Collections.Immutable;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.MultiDimIndexAssignment)]
internal static class MultiDimIndexAssignEvaluator
{
    public static object? Evaluate(BoundMultiDimIndexAssignExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = ctx.Evaluate(node.Target, ct);

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        var value = ctx.Evaluate(node.Value, ct);

        if (node.IsArray)
        {
            var indices = EvaluateIntIndices(node.Indices, ctx, ct);
            ((Array)target).SetValue(value, indices);
            return value;
        }

        if (node.Indexer is { CanWrite: true } indexer)
        {
            var convertedIndices = EvaluateConvertedIndices(node.Indices, indexer, ctx, ct);
            indexer.SetValue(target, value, convertedIndices);
            return value;
        }

        throw new AlderException(
            DiagnosticDescriptors.BadIndexerAccess,
            TypeNameFormatter.Of(target));
    }

    public static async ValueTask<object?> EvaluateAsync(BoundMultiDimIndexAssignExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = await ctx.EvaluateAsync(node.Target, ct);

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        var value = await ctx.EvaluateAsync(node.Value, ct);

        if (node.IsArray)
        {
            var indices = await EvaluateIntIndicesAsync(node.Indices, ctx, ct);
            ((Array)target).SetValue(value, indices);
            return value;
        }

        if (node.Indexer is { CanWrite: true } indexer)
        {
            var convertedIndices = await EvaluateConvertedIndicesAsync(node.Indices, indexer, ctx, ct);
            indexer.SetValue(target, value, convertedIndices);
            return value;
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

    private static object?[] EvaluateConvertedIndices(ImmutableArray<BoundExpr> indexExprs, PropertyInfo indexer, EvaluationContext ctx, CancellationToken ct)
    {
        var indexParams = indexer.GetIndexParameters();
        var converted = new object?[indexExprs.Length];
        for (var i = 0; i < indexExprs.Length; i++)
        {
            var value = ctx.Evaluate(indexExprs[i], ct);
            converted[i] = Convert.ChangeType(value, indexParams[i].ParameterType);
        }
        return converted;
    }

    private static async ValueTask<int[]> EvaluateIntIndicesAsync(ImmutableArray<BoundExpr> indexExprs, EvaluationContext ctx, CancellationToken ct)
    {
        var indices = new int[indexExprs.Length];
        for (var i = 0; i < indexExprs.Length; i++)
            indices[i] = Convert.ToInt32(await ctx.EvaluateAsync(indexExprs[i], ct));
        return indices;
    }

    private static async ValueTask<object?[]> EvaluateConvertedIndicesAsync(ImmutableArray<BoundExpr> indexExprs, PropertyInfo indexer, EvaluationContext ctx, CancellationToken ct)
    {
        var indexParams = indexer.GetIndexParameters();
        var converted = new object?[indexExprs.Length];
        for (var i = 0; i < indexExprs.Length; i++)
        {
            var value = await ctx.EvaluateAsync(indexExprs[i], ct);
            converted[i] = Convert.ChangeType(value, indexParams[i].ParameterType);
        }
        return converted;
    }
}
