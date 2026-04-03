using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.TupleLiteral)]
internal static class TupleEvaluator
{
    public static object? Evaluate(BoundTupleExpr node, EvaluationContext ctx)
    {
        var values = new object?[node.Elements.Length];
        for (var i = 0; i < node.Elements.Length; i++)
            values[i] = ctx.Evaluate(node.Elements[i]);

        var resolvedType = node.StaticType.ClrType;
        var result = resolvedType != typeof(object) && TypeHelpers.IsValueTupleType(resolvedType)
            ? ConstructionRuntime.CreateTupleFromResolvedType(resolvedType, values, ctx.Context)
            : ConstructionRuntime.CreateTuple(values);

        var hasNames = node.ElementNames.Any(static n => n != null);
        if (hasNames)
        {
            var nameMap = new Dictionary<string, int>();
            for (var i = 0; i < node.ElementNames.Length; i++)
            {
                if (node.ElementNames[i] is { } name)
                    nameMap[name] = i;
            }
            return new NamedTupleValue(result, nameMap);
        }

        return result;
    }
}
