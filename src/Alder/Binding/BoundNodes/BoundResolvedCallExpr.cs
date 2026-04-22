using System.Collections.Immutable;
using Alder.Runtime.OverloadResolution;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.ResolvedCall, "ResolvedCall", ManualRewrite = true)]
internal sealed partial record BoundResolvedCallExpr(
    BoundExpr Callee,
    ImmutableArray<BoundExpr> Arguments,
    ResolvedCall Resolution,
    bool IsStaticCall,
    bool IsModuleCall,
    BoundType StaticType,
    bool IsExtensionCall = false) : BoundExpr(StaticType)
{
    internal MethodInfo SelectedMethod => Resolution.Method;
}
