using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundDeconstructionExpr(
    ImmutableArray<string> VariableNames,
    BoundExpr ValueExpression,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.DeconstructionAssignment;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(ValueExpression); }
}
