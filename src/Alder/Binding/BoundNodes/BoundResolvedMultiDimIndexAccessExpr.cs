using System.Collections.Immutable;
using System.Reflection;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundResolvedMultiDimIndexAccessExpr(
    BoundExpr Target,
    ImmutableArray<BoundExpr> Indices,
    Type TargetType,
    bool IsArray,
    PropertyInfo? Indexer,
    bool NullSafe,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.ResolvedMultiDimIndexAccess;
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        visit(Target);
        foreach (var i in Indices) visit(i);
    }
}
