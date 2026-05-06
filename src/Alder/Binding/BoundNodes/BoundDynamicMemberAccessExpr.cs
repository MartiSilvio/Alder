namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.DynamicMemberAccess, "DynamicMemberAccess", ManualRewrite = true)]
internal sealed partial record BoundDynamicMemberAccessExpr(
    BoundExpr Target,
    string MemberName,
    bool NullSafe,
    BoundType StaticType) : BoundMemberAccessBase(Target, MemberName, NullSafe, StaticType);
