using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.AssignmentOperator)]
internal static class AssignEvaluator
{
    public static object? Evaluate(BoundAssignExpr node, EvaluationContext ctx)
    {
        var value = ctx.Evaluate(node.Value);
        value = AssignmentRuntime.ValidateVariableAssignment(node.Name, value, ctx.Context);
        ctx.Context.Set(node.Name, value);
        return value;
    }
}
