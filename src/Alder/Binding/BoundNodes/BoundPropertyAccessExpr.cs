using System.Reflection;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundPropertyAccessExpr(
    BoundExpr Target,
    PropertyInfo Property,
    bool NullSafe,
    bool IsStatic,
    BoundType StaticType) : BoundMemberAccessBase(Target, Property.Name, NullSafe, StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.PropertyAccess;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); }
}
