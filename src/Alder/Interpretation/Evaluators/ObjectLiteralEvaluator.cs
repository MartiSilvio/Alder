using System.Dynamic;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal static class ObjectLiteralEvaluator
{
    public static object? Evaluate(BoundObjectLiteralExpr node, EvaluationContext ctx)
    {
        IDictionary<string, object?> result = new ExpandoObject();
        foreach (var property in node.Properties)
        {
            if (property.IsSpread)
            {
                var spreadValue = ctx.Evaluate(property.Value);
                CollectionFactory.SpreadIntoDict(result, spreadValue, ctx.Context);
                continue;
            }

            result[property.PropertyName!] = ctx.Evaluate(property.Value);
        }

        return result;
    }
}
