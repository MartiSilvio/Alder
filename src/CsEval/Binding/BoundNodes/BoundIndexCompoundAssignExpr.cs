using CsEval.Binding.Plans;
using CsEval.Parsing;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundIndexCompoundAssignExpr(
    BoundExpr Target,
    BoundExpr Index,
    BoundIndexPlan? Plan,
    TokenType Operator,
    BoundExpr Value,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.IndexCompoundAssignment;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); visit(Index); visit(Value); }
}
