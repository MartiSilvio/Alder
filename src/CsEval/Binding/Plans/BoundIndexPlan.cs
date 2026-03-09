namespace CsEval.Binding.Plans;

internal sealed record BoundIndexPlan(
    Type TargetType,
    Type IndexType,
    Type ResultType,
    bool IsDirectCollectionAccess);
