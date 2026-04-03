using System.Dynamic;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.ObjectLiteral)]
internal static class ObjectLiteralEvaluator
{
    public static object? Evaluate(BoundObjectLiteralExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        IDictionary<string, object?> result = new ExpandoObject();
        foreach (var property in node.Properties)
        {
            if (property.IsSpread)
            {
                var spreadValue = ctx.Evaluate(property.Value, ct);
                CollectionFactory.SpreadIntoDict(result, spreadValue, ctx.Context);
                continue;
            }

            result[property.PropertyName!] = ctx.Evaluate(property.Value, ct);
        }

        return result;
    }

    public static async ValueTask<object?> EvaluateAsync(BoundObjectLiteralExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        IDictionary<string, object?> result = new ExpandoObject();
        foreach (var property in node.Properties)
        {
            if (property.IsSpread)
            {
                var spreadValue = await ctx.EvaluateAsync(property.Value, ct);
                CollectionFactory.SpreadIntoDict(result, spreadValue, ctx.Context);
                continue;
            }

            result[property.PropertyName!] = await ctx.EvaluateAsync(property.Value, ct);
        }

        return result;
    }
}
