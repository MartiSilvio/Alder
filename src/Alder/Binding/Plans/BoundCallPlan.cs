using Alder.Runtime;

namespace Alder.Binding.Plans;

internal sealed record BoundCallPlan(
    ResolvedCall Resolution,
    bool IsStaticCall,
    bool IsModuleCall = false)
{
    public MethodInfo SelectedMethod => Resolution.Method;
}
