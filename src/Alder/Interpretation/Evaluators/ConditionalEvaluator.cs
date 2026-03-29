using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal sealed class ConditionalEvaluator : INodeEvaluator<BoundConditionalExpr>
{
    public object? Evaluate(BoundConditionalExpr node, EvaluationContext ctx)
    {
        var condition = ctx.Evaluate(node.Condition);
        var result = TypeHelpers.RequireBoolean(condition)
            ? ctx.Evaluate(node.ThenBranch)
            : ctx.Evaluate(node.ElseBranch);

        var resultType = node.StaticType.ClrType;
        if (result != null && resultType != typeof(object)
            && result.GetType() != resultType && TypeHelpers.IsArithmetic(resultType))
        {
            return NumericDispatch.PromoteToType(result, resultType);
        }

        return result;
    }
}
