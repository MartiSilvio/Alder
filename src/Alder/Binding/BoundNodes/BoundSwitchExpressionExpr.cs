using System.Collections.Immutable;
using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

[BoundContainer]
internal sealed partial record BoundSwitchExpressionArm(
    Pattern Pattern,
    BoundExpr? WhenGuard,
    BoundExpr Value);

[BoundNode(BoundNodeKind.SwitchExpression, "SwitchExpression")]
internal sealed partial record BoundSwitchExpressionExpr(
    BoundExpr Expression,
    ImmutableArray<BoundSwitchExpressionArm> Arms,
    BoundType StaticType) : BoundExpr(StaticType);
