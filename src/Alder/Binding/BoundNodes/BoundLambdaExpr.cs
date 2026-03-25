using System.Collections.Immutable;
using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundLambdaExpr(
    ImmutableArray<string> Parameters,
    Expr Body,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.Lambda;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}
