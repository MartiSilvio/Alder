using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.ArrayAllocation)]
internal static class ArrayAllocationEvaluator
{
    public static object? Evaluate(BoundArrayAllocationExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var sizes = new int[node.Sizes.Length];
        for (var i = 0; i < node.Sizes.Length; i++)
            sizes[i] = Convert.ToInt32(ctx.Evaluate(node.Sizes[i], ct));

        return sizes.Length == 1
            ? RuntimeArrayFactory.Create(node.ElementType, sizes[0], ctx.Context.Config.Security.MaxCollectionSize)
            : RuntimeArrayFactory.Create(node.ElementType, sizes);
    }

    public static async ValueTask<object?> EvaluateAsync(BoundArrayAllocationExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var sizes = new int[node.Sizes.Length];
        for (var i = 0; i < node.Sizes.Length; i++)
            sizes[i] = Convert.ToInt32(await ctx.EvaluateAsync(node.Sizes[i], ct));

        return sizes.Length == 1
            ? RuntimeArrayFactory.Create(node.ElementType, sizes[0], ctx.Context.Config.Security.MaxCollectionSize)
            : RuntimeArrayFactory.Create(node.ElementType, sizes);
    }
}
