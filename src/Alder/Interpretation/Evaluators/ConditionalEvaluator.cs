using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.ConditionalOperator)]
internal static class ConditionalEvaluator
{
    public static object? Evaluate(BoundConditionalExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var condition = ctx.Evaluate(node.Condition, ct);
        var result = TypeHelpers.RequireBoolean(condition)
            ? ctx.Evaluate(node.ThenBranch, ct)
            : ctx.Evaluate(node.ElseBranch, ct);

        var resultType = node.StaticType.ClrType;
        if (result != null && resultType != typeof(object)
            && result.GetType() != resultType && TypeHelpers.IsArithmetic(resultType))
        {
            return NumericDispatch.PromoteToType(result, resultType);
        }

        return result;
    }

    public static async ValueTask<object?> EvaluateAsync(BoundConditionalExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var condition = await ctx.EvaluateAsync(node.Condition, ct);
        var result = TypeHelpers.RequireBoolean(condition)
            ? await ctx.EvaluateAsync(node.ThenBranch, ct)
            : await ctx.EvaluateAsync(node.ElseBranch, ct);

        var resultType = node.StaticType.ClrType;
        if (result != null && resultType != typeof(object)
            && result.GetType() != resultType && TypeHelpers.IsArithmetic(resultType))
        {
            return NumericDispatch.PromoteToType(result, resultType);
        }

        return result;
    }
}
