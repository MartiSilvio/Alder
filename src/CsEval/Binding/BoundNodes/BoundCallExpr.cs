using System.Collections.Immutable;
using CsEval.Binding.Plans;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundCallExpr(
    BoundExpr Callee,
    ImmutableArray<BoundExpr> Arguments,
    BoundCallPlan Plan,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        visit(Callee);
        foreach (var a in Arguments) visit(a);
    }
}
