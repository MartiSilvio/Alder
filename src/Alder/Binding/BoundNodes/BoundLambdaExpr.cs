using System.Collections.Immutable;
using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundLambdaExpr(
    ImmutableArray<string> Parameters,
    Expr Body,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.Lambda;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}
