using System.Reflection;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundFieldAccessExpr(
    BoundExpr Target,
    FieldInfo Field,
    bool NullSafe,
    bool IsStatic,
    BoundType StaticType) : BoundMemberAccessBase(Target, Field.Name, NullSafe, StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.FieldAccess;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); }
}
