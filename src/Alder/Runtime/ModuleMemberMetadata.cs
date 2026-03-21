using System.Diagnostics.CodeAnalysis;
using Alder.Attributes;
using Alder.Runtime.Collections;

namespace Alder.Runtime;

internal static class ModuleMemberMetadata
{
    private static readonly FixedDictionary<string, MemberInfo> BuiltInMathMembersOrdinal =
        Build(typeof(Math), explicitOnly: false, StringComparer.Ordinal);
    private static readonly FixedDictionary<string, MemberInfo> BuiltInMathMembersOrdinalIgnoreCase =
        Build(typeof(Math), explicitOnly: false, StringComparer.OrdinalIgnoreCase);
    private static readonly FixedDictionary<string, MemberInfo> BuiltInConvertMembersOrdinal =
        Build(typeof(Convert), explicitOnly: false, StringComparer.Ordinal);
    private static readonly FixedDictionary<string, MemberInfo> BuiltInConvertMembersOrdinalIgnoreCase =
        Build(typeof(Convert), explicitOnly: false, StringComparer.OrdinalIgnoreCase);

    internal static FixedDictionary<string, MemberInfo> GetBuiltInMathMembers(StringComparer comparer) =>
        ReferenceEquals(comparer, StringComparer.Ordinal)
            ? BuiltInMathMembersOrdinal
            : BuiltInMathMembersOrdinalIgnoreCase;

    internal static FixedDictionary<string, MemberInfo> GetBuiltInConvertMembers(StringComparer comparer) =>
        ReferenceEquals(comparer, StringComparer.Ordinal)
            ? BuiltInConvertMembersOrdinal
            : BuiltInConvertMembersOrdinalIgnoreCase;

    internal static FixedDictionary<string, MemberInfo> Build(
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

            var attr = method.GetCustomAttribute<AlderFunctionAttribute>();
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

        return FixedDictionary<string, MemberInfo>.Create(members, comparer);
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
