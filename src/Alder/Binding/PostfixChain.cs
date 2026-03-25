using Alder.Binding.BoundNodes;

namespace Alder.Binding;

/// <summary>
/// Collects the postfix spine of a bound tree — the alternating
/// MemberAccess / Call / Invoke chain that forms left-recursive nesting like
/// <c>Call(MemberAccess(Call(MemberAccess(...))))</c>.
/// Consumers iterate the collected segments bottom-up instead of recursing.
/// </summary>
internal static class PostfixChain
{
    internal readonly record struct Segment(
        BoundMemberAccessExpr MemberAccess,
        BoundExpr? CallOrInvoke);

    internal readonly record struct Chain(
        IReadOnlyList<Segment> Segments,
        BoundExpr Root);

    /// <summary>
    /// Walks the Callee → Target spine of a bound tree, collecting alternating
    /// Call/Invoke and MemberAccess nodes into a flat segment list.
    /// Returns null if the chain is too short to benefit from iterativization.
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
            if (current is BoundCallExpr call && call.Callee is BoundMemberAccessExpr callMa)
            {
                segments.Add(new Segment(callMa, call));
                current = callMa.Target;
            }
            else if (current is BoundInvokeExpr invoke && invoke.Callee is BoundMemberAccessExpr invokeMa)
            {
                segments.Add(new Segment(invokeMa, invoke));
                current = invokeMa.Target;
            }
            else if (current is BoundMemberAccessExpr ma)
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
            BoundCallExpr c when c.Callee is BoundMemberAccessExpr ma => ma.Target,
            BoundInvokeExpr i when i.Callee is BoundMemberAccessExpr ma => ma.Target,
            BoundMemberAccessExpr ma => ma.Target,
            _ => null
        };

        return inner is BoundCallExpr or BoundInvokeExpr or BoundMemberAccessExpr;
    }
}
