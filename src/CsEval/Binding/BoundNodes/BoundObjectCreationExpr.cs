using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundInitializerEntry(
    string? PropertyName,
    BoundExpr Value,
    BoundExpr? IndexerKey = null);

internal sealed record BoundObjectCreationExpr(
    string TypeName,
    ImmutableArray<BoundExpr> Arguments,
    ImmutableArray<BoundInitializerEntry> InitializerEntries,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.ObjectCreationExpression;
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        foreach (var a in Arguments) visit(a);
        foreach (var e in InitializerEntries)
        {
            visit(e.Value);
            if (e.IndexerKey != null) visit(e.IndexerKey);
        }
    }
}
