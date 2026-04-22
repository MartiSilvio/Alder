using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.AssignmentOperator)]
internal static class AssignEvaluator
{
    public static object? Evaluate(BoundAssignExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var value = ctx.Evaluate(node.Value, ct);

        // ECMA-334 §9.2.9.1: discard. Evaluate RHS, discard result.
        if (node.Name == Parsing.TokenLexemes.DiscardIdentifier && !ctx.Context.TryGet(Parsing.TokenLexemes.DiscardIdentifier, out _))
            return value;

        value = AssignmentRuntime.ValidateVariableAssignment(node.Name, value, ctx.Context);
        ctx.Context.Set(node.Name, value);
        return value;
    }

    public static async ValueTask<object?> EvaluateAsync(BoundAssignExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var value = await ctx.EvaluateAsync(node.Value, ct);

        if (node.Name == Parsing.TokenLexemes.DiscardIdentifier && !ctx.Context.TryGet(Parsing.TokenLexemes.DiscardIdentifier, out _))
            return value;

        value = AssignmentRuntime.ValidateVariableAssignment(node.Name, value, ctx.Context);
        ctx.Context.Set(node.Name, value);
        return value;
    }
}
