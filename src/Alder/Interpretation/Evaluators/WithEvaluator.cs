using Alder.Binding;
using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.WithExpression)]
internal static class WithEvaluator
{
    public static object? Evaluate(BoundWithExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var original = ctx.Evaluate(node.Object, ct);
        var names = new string[node.Initializers.Length];
        var values = new object?[node.Initializers.Length];
        for (var i = 0; i < node.Initializers.Length; i++)
        {
            names[i] = node.Initializers[i].PropertyName;
            values[i] = ctx.Evaluate(node.Initializers[i].Value, ct);
        }
        return Runtime.WithRuntime.ApplyWith(original, names, values, ctx.Context);
    }
}
