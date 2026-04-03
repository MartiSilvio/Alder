using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundInitializerEntry(
    string? PropertyName,
    BoundExpr? Value = null,
    BoundExpr? IndexerKey = null,
    ImmutableArray<BoundExpr> Elements = default);

internal sealed record BoundObjectCreationExpr(
    string TypeName,
    ImmutableArray<BoundExpr> Arguments,
    ImmutableArray<BoundInitializerEntry> InitializerEntries,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.ObjectCreationExpression;
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        foreach (var a in Arguments) visit(a);
        foreach (var e in InitializerEntries)
        {
            if (!e.Elements.IsDefaultOrEmpty)
            {
                foreach (var el in e.Elements) visit(el);
            }
            else if (e.Value != null)
            {
                visit(e.Value);
            }
            if (e.IndexerKey != null) visit(e.IndexerKey);
        }
    }
}
