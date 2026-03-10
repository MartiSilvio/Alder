using System.Collections.Immutable;
using System.Reflection;

namespace CsEval.Binding.Plans;

internal enum BoundParameterBindingKind
{
    Argument = 0,
    DefaultValue = 1,
    ParamsArray = 2
}

internal sealed record BoundConversionPlan(
    Type SourceType,
    Type TargetType,
    bool IsIdentity);

internal sealed record BoundParameterBinding(
    BoundParameterBindingKind Kind,
    int ParameterIndex,
    int SourceArgumentIndex,
    int SourceArgumentCount);

internal sealed record BoundCallPlan(
    MethodInfo SelectedMethod,
    ImmutableArray<BoundConversionPlan> ArgumentConversions,
    ImmutableArray<BoundParameterBinding> ParameterBindings,
    bool IsStaticCall,
    bool IsModuleCall = false);
