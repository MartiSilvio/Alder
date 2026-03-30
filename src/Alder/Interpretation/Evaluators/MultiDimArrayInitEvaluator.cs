using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.MultiDimArrayInit)]
internal static class MultiDimArrayInitEvaluator
{
    public static object? Evaluate(BoundMultiDimArrayInitExpr node, EvaluationContext ctx)
    {
        var dimensions = node.InferredDimensions;
        if (node.ExplicitSizes != null)
        {
            for (var i = 0; i < node.ExplicitSizes.Value.Length; i++)
                dimensions[i] = Convert.ToInt32(ctx.Evaluate(node.ExplicitSizes.Value[i]));
        }

        var array = RuntimeArrayFactory.Create(node.ElementType, dimensions);

        // Fill the array from the flat value list using row-major order
        var indices = new int[node.Rank];
        for (var i = 0; i < node.FlatValues.Length; i++)
        {
            var value = ctx.Evaluate(node.FlatValues[i]);
            if (value != null)
                value = Convert.ChangeType(value, node.ElementType);
            array.SetValue(value, indices);

            // Increment indices (rightmost first)
            for (var d = node.Rank - 1; d >= 0; d--)
            {
                indices[d]++;
                if (indices[d] < dimensions[d])
                    break;
                indices[d] = 0;
            }
        }

        return array;
    }
}
