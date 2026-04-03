using System.Threading.Tasks;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.VariableDeclaration)]
internal static class VariableDeclEvaluator
{
    public static object? Evaluate(BoundVariableDeclExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var value = ctx.Evaluate(node.Initializer, ct);
        return AssignmentRuntime.DefineVariable(
            node.Name,
            value,
            node.DeclaredType,
            ctx.Context,
            node.IsConst,
            isConstantExpression: BoundExpr.IsConstantExpression(node.Initializer));
    }

    public static async ValueTask<object?> EvaluateAsync(BoundVariableDeclExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var value = await ctx.EvaluateAsync(node.Initializer, ct);
        return AssignmentRuntime.DefineVariable(
            node.Name,
            value,
            node.DeclaredType,
            ctx.Context,
            node.IsConst,
            isConstantExpression: BoundExpr.IsConstantExpression(node.Initializer));
    }
}
