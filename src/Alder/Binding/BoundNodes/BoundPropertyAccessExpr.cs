namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.PropertyAccess, "PropertyAccess", ManualRewrite = true)]
internal sealed partial record BoundPropertyAccessExpr(
    BoundExpr Target,
    PropertyInfo Property,
    bool NullSafe,
    bool IsStatic,
    BoundType StaticType) : BoundMemberAccessBase(Target, Property.Name, NullSafe, StaticType);
