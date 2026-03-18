namespace CsEval.Binding.BoundNodes;

internal sealed record BoundAssignExpr(
    string Name,
    BoundExpr Value,
    Type StaticType,
    int? LocalId = null) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Value); }
}
