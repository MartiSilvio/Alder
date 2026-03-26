using System.Reflection;

namespace Alder.Binding.Plans;

internal sealed record BoundMultiDimIndexPlan(
    Type TargetType,
    bool IsArray,
    PropertyInfo? Indexer);
