namespace Alder.Binding.BoundNodes;

internal sealed record BoundVariableDeclExpr(
    string Name,
    BoundExpr Initializer,
    Type? DeclaredType,
    BoundType StaticType,
    bool IsConst = false,
    int? LocalId = null) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.VariableDeclaration;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Initializer); }
}
