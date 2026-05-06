namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.FieldAccess, "FieldAccess", ManualRewrite = true)]
internal sealed partial record BoundFieldAccessExpr(
    BoundExpr Target,
    FieldInfo Field,
    bool NullSafe,
    bool IsStatic,
    BoundType StaticType) : BoundMemberAccessBase(Target, Field.Name, NullSafe, StaticType);
