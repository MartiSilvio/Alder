using System.Collections.Immutable;
using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundSwitchExpressionArm(
    Pattern Pattern,
    BoundExpr? WhenGuard,
    BoundExpr Value);

internal sealed record BoundSwitchExpressionExpr(
    BoundExpr Expression,
    ImmutableArray<BoundSwitchExpressionArm> Arms,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.SwitchExpression;
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        visit(Expression);
        foreach (var a in Arms)
        {
            if (a.WhenGuard != null) visit(a.WhenGuard);
            visit(a.Value);
        }
    }
}
