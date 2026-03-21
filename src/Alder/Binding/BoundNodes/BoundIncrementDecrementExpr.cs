using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundIncrementDecrementExpr(
    string Name,
    TokenType Operator,
    bool IsPrefix,
    Type StaticType,
    int? LocalId = null) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.IncrementOperator;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}
