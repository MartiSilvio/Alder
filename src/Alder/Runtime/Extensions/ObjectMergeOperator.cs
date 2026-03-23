using Alder.Diagnostics;

namespace Alder.Runtime.Extensions;

/// <summary>
/// Object merge via + operator: merges dictionaries and object properties.
/// Called explicitly from Operators.Add() as a fallback for non-arithmetic, non-string operands.
/// </summary>
internal static class ObjectMergeOperator
{
    internal static object? MergeObjects(object? left, object? right, StringComparer comparer, AlderContext? context)
    {
        var merged = new Dictionary<string, object?>(comparer);

        CopyObjectProperties(left, merged, context);
        CopyObjectProperties(right, merged, context);

        if (merged.Count == 0 && (left != null || right != null))
            throw new AlderException(DiagnosticDescriptors.BadBinaryOps, "+", TypeNameFormatter.Of(left), TypeNameFormatter.Of(right));

        return merged;
    }

    private static void CopyObjectProperties(object? obj, Dictionary<string, object?> target, AlderContext? context)
    {
        if (obj == null) return;

        if (obj is IDictionary<string, object?> dict)
        {
            foreach (var kvp in dict)
                target[kvp.Key] = kvp.Value;
            return;
        }

        var type = obj.GetType();
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;

        if (context != null)
        {
            foreach (var prop in context.TypeMetadata.GetProperties(type, bindingFlags))
            {
                if (prop.CanRead)
                    target[prop.Name] = context.TypeMetadata.GetPropertyValue(prop, obj);
            }
        }
        else
        {
            foreach (var prop in ReflectionRuntime.GetProperties(type, bindingFlags))
            {
                if (prop.CanRead)
                    target[prop.Name] = prop.GetValue(obj);
            }
        }
    }
}
