using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.ForStatement, "For")]
internal sealed partial record BoundForExpr(
    ImmutableArray<BoundExpr> Initializers,
    BoundExpr? Condition,
    ImmutableArray<BoundExpr> Increments,
    ImmutableArray<BoundExpr> Body,
    BoundType StaticType) : BoundExpr(StaticType);
