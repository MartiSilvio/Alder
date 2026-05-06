using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.DoStatement, "DoWhile")]
internal sealed partial record BoundDoWhileExpr(
    ImmutableArray<BoundExpr> Body,
    BoundExpr Condition,
    BoundType StaticType) : BoundExpr(StaticType);
