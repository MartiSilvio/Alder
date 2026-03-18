namespace CsEval.Binding.BoundNodes;

internal sealed record BoundReturnExpr(
    BoundExpr? Value,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit) { if (Value != null) visit(Value); }
}
