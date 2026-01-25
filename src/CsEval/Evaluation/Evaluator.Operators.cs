namespace CsEval.Evaluation;

public sealed partial class Evaluator
{
    private static bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            int i => i != 0,
            long l => l != 0,
            double d => d != 0,
            string s => !string.IsNullOrEmpty(s),
            _ => true
        };
    }

    private new static bool Equals(object? left, object? right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;
        if (left.Equals(right)) return true;

        // Let C# runtime handle numeric comparison via dynamic
        if (IsNumeric(left) && IsNumeric(right))
            return (dynamic)left! == (dynamic)right!;

        return false;
    }

    private int Compare(object? left, object? right)
    {
        if (left == null || right == null)
            throw new EvalException("Cannot compare null values");

        // Let C# runtime handle comparison via dynamic
        if (IsNumeric(left) && IsNumeric(right))
        {
            dynamic l = left, r = right;
            return l < r ? -1 : l > r ? 1 : 0;
        }

        return left switch
        {
            string ls when right is string rs => string.Compare(ls, rs, _options.StringComparison),
            IComparable comparable => comparable.CompareTo(right),
            _ => throw new EvalException($"Cannot compare {left.GetType().Name} and {right.GetType().Name}")
        };
    }

    private object? Add(object? left, object? right)
    {
        if (left is string || right is string)
            return $"{left}{right}";

        // Let C# runtime handle numeric addition via dynamic
        if (IsNumeric(left) && IsNumeric(right))
            return (dynamic)left! + (dynamic)right!;

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

        throw new EvalException($"Cannot add {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? Subtract(object? left, object? right)
    {
        if (IsNumeric(left) && IsNumeric(right))
            return (dynamic)left! - (dynamic)right!;

        throw new EvalException($"Cannot subtract {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? Multiply(object? left, object? right)
    {
        if (IsNumeric(left) && IsNumeric(right))
            return (dynamic)left! * (dynamic)right!;

        throw new EvalException($"Cannot multiply {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? Divide(object? left, object? right)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            // Check for division by zero
            if ((dynamic)right! == 0)
                throw new DivideByZeroException();
            return (dynamic)left! / (dynamic)right!;
        }

        throw new EvalException($"Cannot divide {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? Modulo(object? left, object? right)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            if ((dynamic)right! == 0)
                throw new DivideByZeroException();
            return (dynamic)left! % (dynamic)right!;
        }

        throw new EvalException($"Cannot modulo {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? Negate(object? value)
    {
        if (IsNumeric(value))
            return -(dynamic)value!;

        throw new EvalException($"Cannot negate {value?.GetType().Name ?? "null"}");
    }

    private static object? BitwiseNot(object? value)
    {
        if (!IsNumeric(value))
            throw new EvalException($"Cannot apply bitwise NOT to {value?.GetType().Name ?? "null"}");

        return ~(dynamic)value!;
    }

    private static object? BitwiseAnd(object? left, object? right)
    {
        if (!IsNumeric(left) || !IsNumeric(right))
            throw new EvalException($"Cannot apply bitwise AND to {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");

        return (dynamic)left! & (dynamic)right!;
    }

    private static object? BitwiseOr(object? left, object? right)
    {
        if (!IsNumeric(left) || !IsNumeric(right))
            throw new EvalException($"Cannot apply bitwise OR to {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");

        return (dynamic)left! | (dynamic)right!;
    }

    private static object? BitwiseXor(object? left, object? right)
    {
        if (!IsNumeric(left) || !IsNumeric(right))
            throw new EvalException($"Cannot apply bitwise XOR to {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");

        return (dynamic)left! ^ (dynamic)right!;
    }

    private static object? LeftShift(object? left, object? right)
    {
        if (!IsNumeric(left) || !IsNumeric(right))
            throw new EvalException($"Cannot apply left shift to {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");

        return (dynamic)left! << (int)(dynamic)right!;
    }

    private static object? RightShift(object? left, object? right)
    {
        if (!IsNumeric(left) || !IsNumeric(right))
            throw new EvalException($"Cannot apply right shift to {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");

        return (dynamic)left! >> (int)(dynamic)right!;
    }

    private static bool IsNumeric(object? value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;

    /// <summary>
    /// Checks if a value exists in a collection or string.
    /// Supports strings (substring check) and IEnumerable (element check).
    /// </summary>
    private static bool Contains(object? collection, object? value)
    {
        if (collection == null)
            throw new EvalException("Cannot check containment in null collection");

        // String containment: "bc" in "abcd"
        if (collection is string str && value is string substr)
            return str.Contains(substr);

        // Character in string: 'b' in "abc"
        if (collection is string strForChar && value is char ch)
            return strForChar.Contains(ch);

        // Collection containment
        if (collection is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (Equals(item, value))
                    return true;
            }
            return false;
        }

        throw new EvalException($"Cannot use 'in' operator with {collection.GetType().Name}");
    }
}
