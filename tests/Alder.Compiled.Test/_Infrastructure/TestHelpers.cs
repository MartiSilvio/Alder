using System.Reflection;

namespace Alder.Test._Infrastructure;

public static class TestHelpers
{
    public static object? ReadProjectedMember(object? value, string name)
    {
        if (value == null)
            throw new AssertionException($"Cannot read projected member '{name}' from null.");

        if (value is IReadOnlyDictionary<string, object?> readOnlyDict &&
            readOnlyDict.TryGetValue(name, out var dictValue))
        {
            return dictValue;
        }

        var property = value.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (property == null)
            throw new AssertionException($"Projected member '{name}' was not found on '{value.GetType().Name}'.");

        return property.GetValue(value);
    }
}
