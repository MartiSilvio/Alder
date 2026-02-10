using CsEval.Diagnostics;

namespace CsEval.Runtime.Extensions;

/// <summary>
/// Object merge via + operator: merges dictionaries and object properties.
/// Called explicitly from Operators.Add() as a fallback for non-arithmetic, non-string operands.
/// </summary>
internal static class ObjectMergeOperator
{
    internal static object? MergeObjects(object? left, object? right, CsEvalOptions options, CsEvalContext? context)
    {
        var comparer = options.StringComparer;
        var merged = new Dictionary<string, object?>(comparer);

        CopyObjectProperties(left, merged, context);
        CopyObjectProperties(right, merged, context);

        if (merged.Count == 0 && (left != null || right != null))
            throw new CsEvalException(DiagnosticDescriptors.BadBinaryOps, "+", left?.GetType().Name ?? "null", right?.GetType().Name ?? "null");

        return merged;
    }

    private static void CopyObjectProperties(object? obj, Dictionary<string, object?> target, CsEvalContext? context)
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
            foreach (var prop in context.TypeCache.GetProperties(type, bindingFlags))
            {
                if (prop.CanRead)
                    target[prop.Name] = context.TypeCache.GetPropertyValue(prop, obj);
            }
        }
        else
        {
            foreach (var prop in type.GetProperties(bindingFlags))
            {
                if (prop.CanRead)
                    target[prop.Name] = prop.GetValue(obj);
            }
        }
    }
}
