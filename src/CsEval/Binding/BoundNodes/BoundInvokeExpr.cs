using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundInvokeExpr(
    BoundExpr Callee,
    ImmutableArray<BoundExpr> Arguments,
    ImmutableArray<string> TypeArguments,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        visit(Callee);
        foreach (var a in Arguments) visit(a);
    }
}
