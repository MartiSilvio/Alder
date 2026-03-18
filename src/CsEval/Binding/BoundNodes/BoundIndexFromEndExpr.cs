namespace CsEval.Binding.BoundNodes;

// ^expr — creates System.Index(expr, fromEnd: true)
internal sealed record BoundIndexFromEndExpr(
    BoundExpr Operand,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Operand); }
}
