namespace Alder.Binding.BoundNodes;

internal abstract record BoundMemberAccessBase(
    BoundExpr Target,
    string MemberName,
    bool NullSafe,
    BoundType StaticType) : BoundExpr(StaticType);
