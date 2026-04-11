using Alder.Binding.BoundNodes;

namespace Alder.Binding;

/// <summary>
/// Collects the postfix spine of a bound tree into an iterative representation.
/// This avoids deep recursion on member-access and call chains.
/// Consumers iterate the collected segments bottom-up instead of recursing.
/// </summary>
internal static class PostfixChain
{
    internal readonly record struct Segment(
        BoundMemberAccessBase MemberAccess,
        BoundExpr? CallOrInvoke);

    internal readonly record struct Chain(
        IReadOnlyList<Segment> Segments,
        BoundExpr Root);

    /// <summary>
    /// Walks the callee-to-target spine of a bound tree and flattens alternating
    /// call and member-access nodes into a segment list.
    /// Returns null if the chain is too short to benefit from iterative processing.
    /// Segments are ordered outside-in; process from <c>Count - 1</c> down to <c>0</c>.
    /// </summary>
    internal static Chain? TryCollect(BoundExpr node)
    {
        if (!IsChainStart(node))
            return null;

        var segments = new List<Segment>();
        var current = node;

        while (true)
        {
            if (current is BoundResolvedCallExpr { Callee: BoundMemberAccessBase callMa } call)
            {
                segments.Add(new Segment(callMa, call));
                current = callMa.Target;
            }
            else if (current is BoundDynamicCallExpr { Callee: BoundMemberAccessBase invokeMa } invoke)
            {
                segments.Add(new Segment(invokeMa, invoke));
                current = invokeMa.Target;
            }
            else if (current is BoundMemberAccessBase ma)
            {
                segments.Add(new Segment(ma, null));
                current = ma.Target;
            }
            else
            {
                break;
            }
        }

        return segments.Count > 1 ? new Chain(segments, current) : null;
    }

    private static bool IsChainStart(BoundExpr node)
    {
        var inner = node switch
        {
            BoundResolvedCallExpr { Callee: BoundMemberAccessBase ma } => ma.Target,
            BoundDynamicCallExpr { Callee: BoundMemberAccessBase ma } => ma.Target,
            BoundMemberAccessBase ma => ma.Target,
            _ => null
        };

        return inner is BoundResolvedCallExpr or BoundDynamicCallExpr or BoundMemberAccessBase;
    }
}
