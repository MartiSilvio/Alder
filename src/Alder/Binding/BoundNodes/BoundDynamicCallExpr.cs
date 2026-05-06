using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.DynamicCall, "DynamicCall", ManualRewrite = true)]
internal sealed partial record BoundDynamicCallExpr(
    BoundExpr Callee,
    ImmutableArray<BoundExpr> Arguments,
    ImmutableArray<string> TypeArguments,
    BoundType StaticType) : BoundExpr(StaticType);
