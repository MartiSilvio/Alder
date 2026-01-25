using System.Collections;
using CsEval.Parsing;

namespace CsEval.Evaluation;

public sealed partial class Evaluator
{
    private object? Add(object? left, object? right)
    {
        // String concatenation or numeric addition
        if (left is string || right is string || (RuntimeHelpers.IsNumeric(left) && RuntimeHelpers.IsNumeric(right)))
            return RuntimeHelpers.Add(left, right, _options);

        // Both are dictionaries
        if (left is IDictionary<string, object?> leftDict && right is IDictionary<string, object?> rightDict)
        {
            var comparer = _options.StringComparer;
            var merged = new Dictionary<string, object?>(comparer);
            foreach (var kvp in leftDict)
                merged[kvp.Key] = kvp.Value;
            foreach (var kvp in rightDict)
                merged[kvp.Key] = kvp.Value;
            return merged;
        }

        // Left is a typed object, right is a dictionary - merge by reflecting left's properties
        if (left != null && right is IDictionary<string, object?> rightDictOnly)
        {
            var comparer = _options.StringComparer;
            var merged = new Dictionary<string, object?>(comparer);

            // Copy properties from the left object via compiled getters
            var leftType = left.GetType();
            foreach (var prop in _context.TypeCache.GetProperties(leftType, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (prop.CanRead)
                    merged[prop.Name] = _context.TypeCache.GetPropertyValue(prop, left);
            }

            // Override/add properties from the right dictionary
            foreach (var kvp in rightDictOnly)
                merged[kvp.Key] = kvp.Value;

            return merged;
        }

        // Left is a dictionary, right is a typed object - merge by reflecting right's properties
        if (left is IDictionary<string, object?> leftDictOnly && right != null)
        {
            var comparer = _options.StringComparer;
            var merged = new Dictionary<string, object?>(comparer);

            // Copy properties from the left dictionary
            foreach (var kvp in leftDictOnly)
                merged[kvp.Key] = kvp.Value;

            // Override/add properties from the right object via compiled getters
            var rightType = right.GetType();
            foreach (var prop in _context.TypeCache.GetProperties(rightType, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (prop.CanRead)
                    merged[prop.Name] = _context.TypeCache.GetPropertyValue(prop, right);
            }

            return merged;
        }

        // Both are typed objects - merge by reflecting both
        if (left != null && right != null)
        {
            var comparer = _options.StringComparer;
            var merged = new Dictionary<string, object?>(comparer);

            // Copy properties from the left object via compiled getters
            var leftType = left.GetType();
            foreach (var prop in _context.TypeCache.GetProperties(leftType, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (prop.CanRead)
                    merged[prop.Name] = _context.TypeCache.GetPropertyValue(prop, left);
            }

            // Override/add properties from the right object via compiled getters
            var rightType = right.GetType();
            foreach (var prop in _context.TypeCache.GetProperties(rightType, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (prop.CanRead)
                    merged[prop.Name] = _context.TypeCache.GetPropertyValue(prop, right);
            }

            return merged;
        }

        throw new CsEvalException($"Cannot add {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }
}
