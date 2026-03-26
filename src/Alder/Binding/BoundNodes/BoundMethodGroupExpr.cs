namespace Alder.Binding.BoundNodes;

internal sealed record BoundMethodGroupExpr(
    BoundExpr Target,
    Type DeclaringType,
    string MethodName,
    bool NullSafe,
    bool IsStatic,
    BoundType StaticType) : BoundMemberAccessBase(Target, MethodName, NullSafe, StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.MethodGroup;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); }
}
