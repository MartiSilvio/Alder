using CsEval.Binding.Plans;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundIndexAccessExpr(
    BoundExpr Target,
    BoundExpr Index,
    BoundIndexPlan? Plan,
    bool NullSafe,
    Type StaticType) : BoundExpr(StaticType);
