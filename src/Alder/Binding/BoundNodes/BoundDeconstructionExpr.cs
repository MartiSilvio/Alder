using System.Collections.Immutable;
using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.DeconstructionAssignment, "Deconstruction")]
internal sealed partial record BoundDeconstructionExpr(
    DeconstructionExpr Source,
    BoundExpr ValueExpression,
    BoundType StaticType) : BoundExpr(StaticType)
{
    public ImmutableArray<string> VariableNames { get; } = [..Source.VariableNames];
}
