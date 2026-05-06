using Alder.Binding;
using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.TypeReference)]
internal static class TypeRefEvaluator
{
    public static object? Evaluate(BoundTypeRefExpr node, EvaluationContext ctx, CancellationToken ct) =>
        node.TargetType;

    public static ValueTask<object?> EvaluateAsync(BoundTypeRefExpr node, EvaluationContext ctx, CancellationToken ct) =>
        new(node.TargetType);
}
