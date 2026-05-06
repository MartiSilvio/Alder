using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Alder.Attributes;
using Alder.Runtime.Collections;

namespace Alder.Runtime;

internal sealed record ModuleMemberEntry(
    IReadOnlyList<MethodInfo> Methods,
    PropertyInfo? Property,
    FieldInfo? Field)
{
    public bool HasMethods => Methods.Count > 0;
}

internal static class ModuleMemberMetadata
{
    private static readonly FixedDictionary<string, ModuleMemberEntry> BuiltInMathMembersOrdinal =
        Build(typeof(Math), explicitOnly: false, StringComparer.Ordinal);
    private static readonly FixedDictionary<string, ModuleMemberEntry> BuiltInMathMembersOrdinalIgnoreCase =
        Build(typeof(Math), explicitOnly: false, StringComparer.OrdinalIgnoreCase);
    private static readonly FixedDictionary<string, ModuleMemberEntry> BuiltInConvertMembersOrdinal =
        Build(typeof(Convert), explicitOnly: false, StringComparer.Ordinal);
    private static readonly FixedDictionary<string, ModuleMemberEntry> BuiltInConvertMembersOrdinalIgnoreCase =
        Build(typeof(Convert), explicitOnly: false, StringComparer.OrdinalIgnoreCase);

    internal static FixedDictionary<string, ModuleMemberEntry> GetBuiltInMathMembers(StringComparer comparer) =>
        ReferenceEquals(comparer, StringComparer.Ordinal)
            ? BuiltInMathMembersOrdinal
            : BuiltInMathMembersOrdinalIgnoreCase;

    internal static FixedDictionary<string, ModuleMemberEntry> GetBuiltInConvertMembers(StringComparer comparer) =>
        ReferenceEquals(comparer, StringComparer.Ordinal)
            ? BuiltInConvertMembersOrdinal
            : BuiltInConvertMembersOrdinalIgnoreCase;

    internal static FixedDictionary<string, ModuleMemberEntry> Build(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        bool explicitOnly,
        StringComparer comparer)
    {
        var methodsByName = new Dictionary<string, List<MethodInfo>>(comparer);
        var propertiesByName = new Dictionary<string, PropertyInfo>(comparer);
        var fieldsByName = new Dictionary<string, FieldInfo>(comparer);

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            if (method.IsSpecialName)
                continue;

            var attr = method.GetCustomAttribute<AlderFunctionAttribute>();
            if (explicitOnly && attr == null)
                continue;

            var name = attr?.Name ?? method.Name;
            if (!methodsByName.TryGetValue(name, out var methods))
            {
                methods = [];
                methodsByName[name] = methods;
            }

            methods.Add(method);
        }

        if (!explicitOnly)
        {
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                propertiesByName[prop.Name] = prop;

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                fieldsByName[field.Name] = field;
        }

        var members = new Dictionary<string, ModuleMemberEntry>(comparer);

        foreach (var (name, methods) in methodsByName)
        {
            propertiesByName.TryGetValue(name, out var property);
            fieldsByName.TryGetValue(name, out var field);
            members[name] = new ModuleMemberEntry(methods.ToArray(), property, field);
        }

        foreach (var (name, property) in propertiesByName)
        {
            if (members.ContainsKey(name))
                continue;

            fieldsByName.TryGetValue(name, out var field);
            members[name] = new ModuleMemberEntry([], property, field);
        }

        foreach (var (name, field) in fieldsByName)
        {
            if (members.ContainsKey(name))
                continue;

            members[name] = new ModuleMemberEntry([], null, field);
        }

        return FixedDictionary<string, ModuleMemberEntry>.Create(members, comparer);
    }

    internal static FixedDictionary<string, ModuleMemberEntry> BuildFromMemberMap(
        IReadOnlyDictionary<string, IReadOnlyCollection<MemberInfo>> members,
        StringComparer comparer)
    {
        var normalized = new Dictionary<string, ModuleMemberEntry>(comparer);

        foreach (var (name, rawMembers) in members)
        {
            var methods = new List<MethodInfo>();
            PropertyInfo? property = null;
            FieldInfo? field = null;

            foreach (var member in rawMembers)
            {
                switch (member)
                {
                    case MethodInfo method:
                        methods.Add(method);
                        break;
                    case PropertyInfo prop when property is null && field is null && methods.Count == 0:
                        property = prop;
                        break;
                    case FieldInfo moduleField when field is null && property is null && methods.Count == 0:
                        field = moduleField;
                        break;
                    case PropertyInfo:
                    case FieldInfo:
                        throw new ArgumentException(
                            $"Module member '{name}' must expose either methods or a single property/field.",
                            nameof(members));
                    default:
                        throw new ArgumentException(
                            $"Module member '{name}' uses unsupported member type '{member.GetType().Name}'.",
                            nameof(members));
                }
            }

            if (methods.Count == 0 && property is null && field is null)
                throw new ArgumentException($"Module member '{name}' must expose at least one member.", nameof(members));

            normalized[name] = new ModuleMemberEntry(
                new ReadOnlyCollection<MethodInfo>(methods),
                property,
                field);
        }

        return FixedDictionary<string, ModuleMemberEntry>.Create(normalized, comparer);
    }
}
