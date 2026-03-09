using System.Reflection;

namespace CsEval.Binding.Plans;

internal sealed record BoundMemberPlan(
    Type DeclaringType,
    string MemberName,
    MemberInfo? Member,
    bool IsMethodGroup,
    bool IsStatic);
