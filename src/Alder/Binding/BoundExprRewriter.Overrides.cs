using Alder.Binding.BoundNodes;

namespace Alder.Binding;

// Hand-written rewriter overrides for nodes participating in PostfixChain optimization.
// These 6 nodes are marked ManualRewrite = true on their [BoundNode] attribute.
internal abstract partial class BoundExprRewriter
{
    protected override BoundExpr VisitPropertyAccess(BoundPropertyAccessExpr node) => VisitMemberAccess(node);
    protected override BoundExpr VisitFieldAccess(BoundFieldAccessExpr node) => VisitMemberAccess(node);
    protected override BoundExpr VisitMethodGroup(BoundMethodGroupExpr node) => VisitMemberAccess(node);
    protected override BoundExpr VisitDynamicMemberAccess(BoundDynamicMemberAccessExpr node) => VisitMemberAccess(node);

    private BoundExpr VisitMemberAccess(BoundMemberAccessBase node)
    {
        var postfix = PostfixChain.TryCollect(node);
        if (postfix != null)
            return RewritePostfixChain(postfix.Value, node);

        var target = Visit(node.Target);
        if (ReferenceEquals(target, node.Target)) return node;
        return CopyMetadata(node, RewriteMemberAccessTarget(node, target));
    }

    protected override BoundExpr VisitResolvedCall(BoundResolvedCallExpr node)
    {
        var postfix = PostfixChain.TryCollect(node);
        if (postfix != null)
            return RewritePostfixChain(postfix.Value, node);

        var callee = Visit(node.Callee);
        var args = RewriteImmutableArray(node.Arguments, out var changed);
        if (ReferenceEquals(callee, node.Callee) && !changed) return node;
        return CopyMetadata(node, node with { Callee = callee, Arguments = args });
    }

    protected override BoundExpr VisitDynamicCall(BoundDynamicCallExpr node)
    {
        var postfix = PostfixChain.TryCollect(node);
        if (postfix != null)
            return RewritePostfixChain(postfix.Value, node);

        var callee = Visit(node.Callee);
        var args = RewriteImmutableArray(node.Arguments, out var changed);
        if (ReferenceEquals(callee, node.Callee) && !changed) return node;
        return CopyMetadata(node, node with { Callee = callee, Arguments = args });
    }

    private BoundExpr RewritePostfixChain(PostfixChain.Chain chain, BoundExpr originalRoot)
    {
        var current = Visit(chain.Root);
        var anyChanged = !ReferenceEquals(current, chain.Root);

        for (var i = chain.Segments.Count - 1; i >= 0; i--)
        {
            var seg = chain.Segments[i];
            BoundMemberAccessBase originalMa = seg.MemberAccess;

            if (!ReferenceEquals(current, originalMa.Target))
            {
                current = CopyMetadata(originalMa, RewriteMemberAccessTarget(originalMa, current));
                anyChanged = true;
            }
            else
            {
                current = originalMa;
            }

            if (seg.CallOrInvoke is BoundResolvedCallExpr call)
            {
                var args = RewriteImmutableArray(call.Arguments, out var argsChanged);
                if (argsChanged || !ReferenceEquals(current, call.Callee))
                {
                    current = CopyMetadata(call, call with { Callee = current, Arguments = args });
                    anyChanged = true;
                }
                else
                {
                    current = call;
                }
            }
            else if (seg.CallOrInvoke is BoundDynamicCallExpr invoke)
            {
                var args = RewriteImmutableArray(invoke.Arguments, out var argsChanged);
                if (argsChanged || !ReferenceEquals(current, invoke.Callee))
                {
                    current = CopyMetadata(invoke, invoke with { Callee = current, Arguments = args });
                    anyChanged = true;
                }
                else
                {
                    current = invoke;
                }
            }
        }

        return anyChanged ? current : originalRoot;
    }

    private static BoundMemberAccessBase RewriteMemberAccessTarget(BoundMemberAccessBase ma, BoundExpr newTarget) => ma switch
    {
        BoundPropertyAccessExpr prop => prop with { Target = newTarget },
        BoundFieldAccessExpr field => field with { Target = newTarget },
        BoundMethodGroupExpr mg => mg with { Target = newTarget },
        BoundDynamicMemberAccessExpr dyn => dyn with { Target = newTarget },
        _ => throw new InvalidOperationException($"Unexpected member access type '{ma.GetType().Name}'")
    };
}
