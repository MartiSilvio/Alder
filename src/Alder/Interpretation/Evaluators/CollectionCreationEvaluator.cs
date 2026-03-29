using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal sealed class CollectionCreationEvaluator : INodeEvaluator<BoundCollectionCreationExpr>
{
    public object? Evaluate(BoundCollectionCreationExpr node, EvaluationContext ctx)
    {
        var values = new List<object?>(node.Elements.Length);
        foreach (var element in node.Elements)
        {
            if (element is BoundSpreadExpr spread)
            {
                var spreadValue = ctx.Evaluate(spread.Expression);
                CollectionFactory.SpreadIntoList(values, spreadValue);
            }
            else
            {
                values.Add(ctx.Evaluate(element));
            }
        }

        return node.CollectionKind switch
        {
            CollectionKind.Array => RuntimeArrayFactory.CreateFromValues(node.ElementType, values),
            CollectionKind.InferredArray => RuntimeArrayFactory.InferAndCreateArray(values),
            CollectionKind.TargetTypedCollection => CollectionFactory.Create(
                node.TargetCollectionType!, node.ElementType, values),
            _ => throw new InvalidOperationException()
        };
    }
}
