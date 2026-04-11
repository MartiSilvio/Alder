using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.WhileStatement, "While")]
internal sealed partial record BoundWhileExpr(
    BoundExpr Condition,
    ImmutableArray<BoundExpr> Body,
    BoundType StaticType) : BoundExpr(StaticType);
