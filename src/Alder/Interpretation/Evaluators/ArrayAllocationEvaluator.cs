using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal sealed class ArrayAllocationEvaluator : INodeEvaluator<BoundArrayAllocationExpr>
{
    public object? Evaluate(BoundArrayAllocationExpr node, EvaluationContext ctx)
    {
        var sizes = new int[node.Sizes.Length];
        for (var i = 0; i < node.Sizes.Length; i++)
            sizes[i] = Convert.ToInt32(ctx.Evaluate(node.Sizes[i]));

        return sizes.Length == 1
            ? RuntimeArrayFactory.Create(node.ElementType, sizes[0], ctx.Config.Security.MaxCollectionSize)
            : RuntimeArrayFactory.Create(node.ElementType, sizes);
    }
}
