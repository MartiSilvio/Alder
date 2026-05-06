namespace Alder.Binding.BoundNodes;

internal abstract partial record BoundMemberAccessBase(
    BoundExpr Target,
    string MemberName,
    bool NullSafe,
    BoundType StaticType) : BoundExpr(StaticType);
