using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundDynamicCallExpr(
    BoundExpr Callee,
    ImmutableArray<BoundExpr> Arguments,
    ImmutableArray<string> TypeArguments,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.DynamicCall;
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        visit(Callee);
        foreach (var a in Arguments) visit(a);
    }
}
