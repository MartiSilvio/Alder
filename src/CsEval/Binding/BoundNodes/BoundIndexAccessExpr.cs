using CsEval.Binding.Plans;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundIndexAccessExpr(
    BoundExpr Target,
    BoundExpr Index,
    BoundIndexPlan Plan,
    Type StaticType) : BoundExpr(StaticType);
