using System.Collections.Immutable;
using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundDeconstructionExpr(
    DeconstructionExpr Source,
    BoundExpr ValueExpression,
    BoundType StaticType) : BoundExpr(StaticType)
{
    public ImmutableArray<string> VariableNames { get; } = [..Source.VariableNames];

    internal override BoundNodeKind Kind => BoundNodeKind.DeconstructionAssignment;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(ValueExpression); }
}
