using System.Reflection;
using System.Runtime.CompilerServices;

namespace Alder.Parity;

/// <summary>
/// Shared structural-projection helpers for interpreter↔compiled (and JIT↔AOT) parity comparison.
/// Anonymous-type results and string-keyed dictionaries are compared property-by-property, so both
/// the in-process tests (ParityTests) and the AOT parity harness read object shapes the same way.
/// The equality decision itself is left to each caller (NUnit asserts vs canonical-render compare).
/// </summary>
internal static class StructuralParity
{
    public static bool IsAnonymousType(Type? type) =>
        type != null
        && Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute))
        && type.Name.Contains("AnonymousType");

    public static bool TryReadStructuralParityProperties(
        object? expected,
        object? result,
        out IReadOnlyDictionary<string, object?> expectedProperties,
        out IReadOnlyDictionary<string, object?> actualProperties)
    {
        expectedProperties = null!;
        actualProperties = null!;

        if (expected == null || !TryReadObjectProperties(result, out actualProperties))
            return false;

        if (IsAnonymousType(expected.GetType()))
            return TryReadObjectProperties(expected, out expectedProperties);

        return false;
    }

    public static bool TryReadObjectProperties(object? value, out IReadOnlyDictionary<string, object?> properties)
    {
        properties = null!;
        if (value is null or Type)
            return false;

        if (value is IDictionary<string, object?> dict)
        {
            properties = new Dictionary<string, object?>(dict);
            return true;
        }

        if (value is IReadOnlyDictionary<string, object?> readOnlyDict)
        {
            properties = readOnlyDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return true;
        }

        var readableProperties = value.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .ToArray();
        if (readableProperties.Length == 0)
            return false;

        var propertyValues = new Dictionary<string, object?>();
        foreach (var property in readableProperties)
            propertyValues[property.Name] = property.GetValue(value);

        properties = propertyValues;
        return true;
    }
}
