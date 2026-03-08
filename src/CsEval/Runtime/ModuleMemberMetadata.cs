using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CsEval.Attributes;

namespace CsEval.Runtime;

internal static class ModuleMemberMetadata
{
    private static readonly FrozenDictionary<string, MemberInfo> BuiltInMathMembersOrdinal =
        Build(typeof(Math), explicitOnly: false, StringComparer.Ordinal);
    private static readonly FrozenDictionary<string, MemberInfo> BuiltInMathMembersOrdinalIgnoreCase =
        Build(typeof(Math), explicitOnly: false, StringComparer.OrdinalIgnoreCase);
    private static readonly FrozenDictionary<string, MemberInfo> BuiltInConvertMembersOrdinal =
        Build(typeof(Convert), explicitOnly: false, StringComparer.Ordinal);
    private static readonly FrozenDictionary<string, MemberInfo> BuiltInConvertMembersOrdinalIgnoreCase =
        Build(typeof(Convert), explicitOnly: false, StringComparer.OrdinalIgnoreCase);

    internal static FrozenDictionary<string, MemberInfo> GetBuiltInMathMembers(StringComparer comparer) =>
        comparer == StringComparer.Ordinal
            ? BuiltInMathMembersOrdinal
            : BuiltInMathMembersOrdinalIgnoreCase;

    internal static FrozenDictionary<string, MemberInfo> GetBuiltInConvertMembers(StringComparer comparer) =>
        comparer == StringComparer.Ordinal
            ? BuiltInConvertMembersOrdinal
            : BuiltInConvertMembersOrdinalIgnoreCase;

    internal static FrozenDictionary<string, MemberInfo> Build(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        bool explicitOnly,
        StringComparer comparer)
    {
        var members = new Dictionary<string, MemberInfo>(comparer);

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            if (method.IsSpecialName || IsAsyncMethod(method))
                continue;

            var attr = method.GetCustomAttribute<CsEvalFunctionAttribute>();
            if (explicitOnly && attr == null)
                continue;

            var name = attr?.Name ?? method.Name;
            members[name] = method;
        }

        if (!explicitOnly)
        {
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                members[prop.Name] = prop;

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                members[field.Name] = field;
        }

        return members.ToFrozenDictionary(comparer);
    }

    private static bool IsAsyncMethod(MethodInfo method)
    {
        var returnType = method.ReturnType;
        if (returnType == typeof(Task) || returnType == typeof(ValueTask))
            return true;

        if (!returnType.IsGenericType)
            return false;

        var genericType = returnType.GetGenericTypeDefinition();
        return genericType == typeof(Task<>) || genericType == typeof(ValueTask<>);
    }
}
