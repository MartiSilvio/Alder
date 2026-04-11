using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Pipeline;

namespace Alder.Binding;

internal abstract partial class BoundExprRewriter : BoundExprVisitor<BoundExpr>, IBoundTreePass
{
    public BoundExpr Rewrite(BoundExpr tree) => Visit(tree);

    BoundExpr IBoundTreePass.Execute(BoundExpr tree, PipelineContext context) => Rewrite(tree);

    protected override BoundExpr DefaultVisit(BoundExpr node) => node;

    private BoundExpr CopyMetadata(BoundExpr original, BoundExpr rewritten)
    {
        if (ReferenceEquals(original, rewritten)) return rewritten;
        rewritten.Span = original.Span;
        if (original.HasErrors) rewritten.HasErrors = true;
        if (original.Diagnostic != null) rewritten.Diagnostic = original.Diagnostic;
        return rewritten;
    }

    private ImmutableArray<BoundExpr> RewriteImmutableArray(ImmutableArray<BoundExpr> items, out bool changed)
    {
        changed = false;
        var builder = ImmutableArray.CreateBuilder<BoundExpr>(items.Length);
        foreach (var item in items)
        {
            var rewritten = Visit(item);
            if (!ReferenceEquals(rewritten, item)) changed = true;
            builder.Add(rewritten);
        }
        return changed ? builder.MoveToImmutable() : items;
    }
}
