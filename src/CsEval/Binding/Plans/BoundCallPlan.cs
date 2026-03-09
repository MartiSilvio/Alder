using System.Collections.Immutable;
using System.Reflection;

namespace CsEval.Binding.Plans;

internal sealed record BoundConversionPlan(
    Type SourceType,
    Type TargetType,
    bool IsIdentity);

internal sealed record BoundCallPlan(
    MethodInfo SelectedMethod,
    ImmutableArray<BoundConversionPlan> ArgumentConversions,
    bool IsStaticCall,
    bool IsModuleCall = false);
