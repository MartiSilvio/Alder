using System.Collections.Immutable;
using Alder.Runtime;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundResolvedCallExpr(
    BoundExpr Callee,
    ImmutableArray<BoundExpr> Arguments,
    ResolvedCall Resolution,
    bool IsStaticCall,
    bool IsModuleCall,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal MethodInfo SelectedMethod => Resolution.Method;
    internal override BoundNodeKind Kind => BoundNodeKind.ResolvedCall;
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        visit(Callee);
        foreach (var a in Arguments) visit(a);
    }
}
