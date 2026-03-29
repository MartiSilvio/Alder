using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

internal static class VariableDeclEvaluator
{
    public static object? Evaluate(BoundVariableDeclExpr node, EvaluationContext ctx)
    {
        var value = ctx.Evaluate(node.Initializer);
        return AssignmentRuntime.DefineVariable(
            node.Name,
            value,
            node.DeclaredType,
            ctx.Context,
            node.IsConst,
            isConstantExpression: BoundExpr.IsConstantExpression(node.Initializer));
    }
}
