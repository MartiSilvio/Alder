namespace CsEval.Binding.BoundNodes;

internal sealed record BoundOutArgExpr(
    string VariableName,
    string? TypeName,
    bool IsDiscard,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.OutArgument;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}
