using System.Collections.Immutable;
using Alder.Binding.Plans;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundCallExpr(
    BoundExpr Callee,
    ImmutableArray<BoundExpr> Arguments,
    BoundCallPlan Plan,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.Call;
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        visit(Callee);
        foreach (var a in Arguments) visit(a);
    }
}
