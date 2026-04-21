namespace Alder.Runtime.Introspection;

#pragma warning disable IL2067, IL2070, IL2072, IL2075, IL2077
internal static class RuntimeTypeIntrospection
{
    public static PropertyInfo? FindProperty(Type type, string name, BindingFlags flags)
    {
        var comparison = flags.HasFlag(BindingFlags.IgnoreCase)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        PropertyInfo? best = null;
        var bestDistance = int.MaxValue;
        foreach (var property in EnumeratePropertyCandidates(type, flags))
        {
            if (!string.Equals(property.Name, name, comparison))
                continue;

            if (!MatchesProperty(type, property, flags))
                continue;

            var distance = GetDeclaringDistance(type, property.DeclaringType);
            if (distance >= bestDistance)
                continue;

            best = property;
            bestDistance = distance;
            if (distance == 0)
                break;
        }

        return best;
    }

    public static FieldInfo? FindField(Type type, string name, BindingFlags flags)
    {
        var comparison = flags.HasFlag(BindingFlags.IgnoreCase)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        FieldInfo? best = null;
        var bestDistance = int.MaxValue;
        foreach (var field in EnumerateFieldCandidates(type, flags))
        {
            if (!string.Equals(field.Name, name, comparison))
                continue;

            if (!MatchesField(type, field, flags))
                continue;

            var distance = GetDeclaringDistance(type, field.DeclaringType);
            if (distance >= bestDistance)
                continue;

            best = field;
            bestDistance = distance;
            if (distance == 0)
                break;
        }

        return best;
    }

    public static PropertyInfo[] GetProperties(Type type, BindingFlags flags)
    {
        return EnumeratePropertyCandidates(type, flags).Where(property => MatchesProperty(type, property, flags)).ToArray();
    }

    public static MethodInfo[] GetMethods(Type type, BindingFlags flags)
    {
        var results = new List<MethodInfo>();
        var seenSignatures = new HashSet<string>(StringComparer.Ordinal);
        foreach (var method in EnumerateMethodCandidates(type, flags))
        {
            if (!MatchesMethod(type, method, flags))
                continue;

            var signature = BuildMethodSignature(method);
            if (seenSignatures.Add(signature))
                results.Add(method);
        }

        return results.ToArray();
    }

    public static ConstructorInfo[] GetConstructors(Type type, BindingFlags flags) =>
        type.GetConstructors(flags);

    public static MethodInfo? FindMethod(Type type, string name, BindingFlags flags, Type[]? parameterTypes = null)
    {
        var comparison = flags.HasFlag(BindingFlags.IgnoreCase)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        foreach (var method in GetMethods(type, flags))
        {
            if (!string.Equals(method.Name, name, comparison))
                continue;

            if (parameterTypes == null)
                return method;

            var parameters = method.GetParameters();
            if (parameters.Length != parameterTypes.Length)
                continue;

            var allMatch = !parameters.Where((t, i) => t.ParameterType != parameterTypes[i]).Any();

            if (allMatch)
                return method;
        }

        return null;
    }

    public static PropertyInfo? FindIndexer(Type type)
    {
        foreach (var property in GetProperties(type, BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0)
                return property;
        }

        return null;
    }

    public static IEnumerable<Type> GetInterfaces(Type type) =>
        type.GetTypeInfo().ImplementedInterfaces;

    private static PropertyInfo[] EnumeratePropertyCandidates(Type type, BindingFlags flags)
    {
        var effectiveFlags = flags & ~BindingFlags.IgnoreCase;
        var props = type.GetProperties(effectiveFlags);

        if (type.IsInterface)
        {
            var all = new List<PropertyInfo>(props);
            foreach (var iface in GetInterfaces(type))
                all.AddRange(iface.GetProperties(effectiveFlags));
            return all.ToArray();
        }

        return props;
    }

    private static FieldInfo[] EnumerateFieldCandidates(Type type, BindingFlags flags) =>
        type.GetFields(flags & ~BindingFlags.IgnoreCase);

    private static MethodInfo[] EnumerateMethodCandidates(Type type, BindingFlags flags)
    {
        var effectiveFlags = flags & ~BindingFlags.IgnoreCase;
        var methods = type.GetMethods(effectiveFlags);
        if (type.IsInterface)
        {
            var all = new List<MethodInfo>(methods);
            foreach (var iface in GetInterfaces(type))
                all.AddRange(iface.GetMethods(effectiveFlags));
            return all.ToArray();
        }
        return methods;
    }

    private static bool MatchesProperty(Type sourceType, PropertyInfo property, BindingFlags flags)
    {
        var accessor = property.GetMethod ?? property.SetMethod;
        if (accessor == null)
            return false;

        return MatchesMemberCore(sourceType, property.DeclaringType, accessor.IsPublic, accessor.IsStatic, flags);
    }

    private static bool MatchesField(Type sourceType, FieldInfo field, BindingFlags flags) =>
        MatchesMemberCore(sourceType, field.DeclaringType, field.IsPublic, field.IsStatic, flags);

    private static bool MatchesMethod(Type sourceType, MethodInfo method, BindingFlags flags) =>
        MatchesMemberCore(sourceType, method.DeclaringType, method.IsPublic, method.IsStatic, flags);

    private static bool MatchesMemberCore(
        Type sourceType,
        Type? declaringType,
        bool isPublic,
        bool isStatic,
        BindingFlags flags)
    {
        var includePublic = (flags & BindingFlags.Public) != 0;
        var includeNonPublic = (flags & BindingFlags.NonPublic) != 0;
        if (includePublic || includeNonPublic)
        {
            if (isPublic && !includePublic)
                return false;
            if (!isPublic && !includeNonPublic)
                return false;
        }

        var includeInstance = (flags & BindingFlags.Instance) != 0;
        var includeStatic = (flags & BindingFlags.Static) != 0;
        if (includeInstance || includeStatic)
        {
            if (isStatic && !includeStatic)
                return false;
            if (!isStatic && !includeInstance)
                return false;
        }

        if ((flags & BindingFlags.DeclaredOnly) != 0 && declaringType != sourceType)
            return false;

        return true;
    }

    private static string BuildMethodSignature(MethodInfo method)
    {
        var parameters = method.GetParameters();
        var builder = new StringBuilder(method.Name.Length + (parameters.Length + 4) * 24);
        builder.Append(method.Name)
            .Append('|')
            .Append(method.IsStatic ? 'S' : 'I')
            .Append('|')
            .Append(method.GetGenericArguments().Length);

        foreach (var t in parameters)
            builder.Append('|').Append(t.ParameterType.ToString());

        return builder.ToString();
    }

    private static int GetDeclaringDistance(Type sourceType, Type? declaringType)
    {
        if (declaringType == null)
            return int.MaxValue;

        if (declaringType == sourceType)
            return 0;

        var distance = 0;
        for (var current = sourceType; current != null; current = current.BaseType)
        {
            if (current == declaringType)
                return distance;
            distance++;
        }

        if (declaringType.IsAssignableFrom(sourceType))
            return 1;

        return int.MaxValue;
    }
}
#pragma warning restore IL2067, IL2070, IL2072, IL2075, IL2077
