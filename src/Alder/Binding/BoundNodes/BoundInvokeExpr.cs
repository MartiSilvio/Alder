using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundInvokeExpr(
    BoundExpr Callee,
    ImmutableArray<BoundExpr> Arguments,
    ImmutableArray<string> TypeArguments,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.Invoke;
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        visit(Callee);
        foreach (var a in Arguments) visit(a);
    }
}
