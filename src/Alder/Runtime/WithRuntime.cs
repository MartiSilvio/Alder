using Alder.Diagnostics;

namespace Alder.Runtime;

internal static class WithRuntime
{
    public static object ApplyWith(object? original, string[] names, object?[] values,
        AlderContext context)
    {
        if (original == null)
            throw new AlderException(DiagnosticDescriptors.NullMemberAccess, "with", "expression");

        if (original is NamedTupleValue namedTuple)
            return namedTuple.WithModifiedValues(names, values);

        // Anonymous types (represented as ExpandoObject/dictionary): clone and modify
        if (original is IDictionary<string, object?> dict)
            return ApplyWithDictionary(dict, names, values, context);

        var clone = CloneObject(original);
        var type = clone.GetType();
        var flags = BindingFlags.Public | BindingFlags.Instance;
        if (!context.Config.IsCaseSensitive) flags |= BindingFlags.IgnoreCase;

        for (var i = 0; i < names.Length; i++)
        {
            var prop = context.TypeMetadata.GetProperty(type, names[i], flags);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(clone, values[i]);
                continue;
            }

            var field = context.TypeMetadata.GetField(type, names[i], flags);
            if (field != null && !field.IsInitOnly)
            {
                field.SetValue(clone, values[i]);
                continue;
            }

            throw new AlderException(DiagnosticDescriptors.MemberNotFound, type.Name, names[i]);
        }

        return clone;
    }

    private static object ApplyWithDictionary(IDictionary<string, object?> original, string[] names, object?[] values, AlderContext context)
    {
        var clone = new System.Dynamic.ExpandoObject() as IDictionary<string, object?>;
        foreach (var kvp in original)
            clone[kvp.Key] = kvp.Value;

        var comparer = context.Config.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        for (var i = 0; i < names.Length; i++)
        {
            var found = false;
            foreach (var key in clone.Keys)
            {
                if (comparer.Equals(key, names[i]))
                {
                    clone[key] = values[i];
                    found = true;
                    break;
                }
            }

            if (!found)
                throw new AlderException(DiagnosticDescriptors.MemberNotFound, "anonymous type", names[i]);
        }

        return (System.Dynamic.ExpandoObject)clone;
    }

    private static object CloneObject(object original)
    {
        var type = original.GetType();

        if (type.IsValueType)
            return original;

        var cloneMethod = type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance);
        if (cloneMethod != null)
            return cloneMethod.Invoke(original, null)!;

        var memberwiseClone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return memberwiseClone.Invoke(original, null)!;
    }
}
