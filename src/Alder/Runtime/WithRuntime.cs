using Alder.Diagnostics;

namespace Alder.Runtime;

internal static class WithRuntime
{
    public static object ApplyWith(object? original, string[] names, object?[] values,
        AlderConfig config, AlderContext context)
    {
        if (original == null)
            throw new AlderException(DiagnosticDescriptors.NullMemberAccess, "with", "expression");

        var clone = CloneObject(original);
        var type = clone.GetType();
        var flags = BindingFlags.Public | BindingFlags.Instance;
        if (!config.IsCaseSensitive) flags |= BindingFlags.IgnoreCase;

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
