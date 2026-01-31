namespace CsEval.Runtime;

/// <summary>
/// Arithmetic, comparison, and bitwise operators.
/// </summary>
public static class Operators
{
    public static object? Negate(object? value)
    {
        if (TypeHelpers.IsNumeric(value))
            return -(dynamic)value!;

        throw new CsEvalException($"Cannot negate {value?.GetType().Name ?? "null"}");
    }

    public static object? Add(object? left, object? right, CsEvalOptions options) =>
        Add(left, right, options, null);

    public static object? Add(object? left, object? right, CsEvalOptions options, CsEvalContext? context)
    {
        if (left is string || right is string)
            return $"{left}{right}";

        if (TypeHelpers.IsNumeric(left) && TypeHelpers.IsNumeric(right))
            return (dynamic)left! + (dynamic)right!;

        return MergeObjects(left, right, options, context);
    }

    private static object? MergeObjects(object? left, object? right, CsEvalOptions options, CsEvalContext? context)
    {
        var comparer = options.StringComparer;
        var merged = new Dictionary<string, object?>(comparer);

        CopyObjectProperties(left, merged, context);
        CopyObjectProperties(right, merged, context);

        if (merged.Count == 0 && (left != null || right != null))
            throw new CsEvalException($"Cannot add {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");

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

    public static object? Subtract(object? left, object? right, CsEvalOptions options)
    {
        if (TypeHelpers.IsNumeric(left) && TypeHelpers.IsNumeric(right))
            return (dynamic)left! - (dynamic)right!;

        throw new CsEvalException($"Cannot subtract {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    public static object? Multiply(object? left, object? right, CsEvalOptions options)
    {
        if (TypeHelpers.IsNumeric(left) && TypeHelpers.IsNumeric(right))
            return (dynamic)left! * (dynamic)right!;

        throw new CsEvalException($"Cannot multiply {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    public static object? Divide(object? left, object? right, CsEvalOptions options)
    {
        if (TypeHelpers.IsNumeric(left) && TypeHelpers.IsNumeric(right))
        {
            if ((dynamic)right! == 0 && TypeHelpers.IsInteger(left) && TypeHelpers.IsInteger(right))
                throw new DivideByZeroException();
            return (dynamic)left! / (dynamic)right!;
        }

        throw new CsEvalException($"Cannot divide {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    public static object? Modulo(object? left, object? right, CsEvalOptions options)
    {
        if (TypeHelpers.IsNumeric(left) && TypeHelpers.IsNumeric(right))
        {
            if ((dynamic)right! == 0 && TypeHelpers.IsInteger(left) && TypeHelpers.IsInteger(right))
                throw new DivideByZeroException();
            return (dynamic)left! % (dynamic)right!;
        }

        throw new CsEvalException($"Cannot modulo {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    public static object Equals(object? left, object? right, CsEvalOptions options)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;
        if (left.Equals(right)) return true;

        if (TypeHelpers.IsNumeric(left) && TypeHelpers.IsNumeric(right))
        {
            if (InvolvesDecimalAndFloatingPoint(left, right))
            {
                return Convert.ToDouble(left) == Convert.ToDouble(right);
            }
            return (dynamic)left! == (dynamic)right!;
        }

        return false;
    }

    private static bool InvolvesDecimalAndFloatingPoint(object? a, object? b)
    {
        var aIsDecimal = a is decimal;
        var bIsDecimal = b is decimal;
        var aIsFloatingPoint = a is float or double;
        var bIsFloatingPoint = b is float or double;
        return (aIsDecimal && bIsFloatingPoint) || (bIsDecimal && aIsFloatingPoint);
    }

    public static object NotEquals(object? left, object? right, CsEvalOptions options)
    {
        return !(bool)Equals(left, right, options);
    }

    public static object LessThan(object? left, object? right, CsEvalOptions options)
    {
        return Compare(left, right, options) < 0;
    }

    public static object LessThanOrEqual(object? left, object? right, CsEvalOptions options)
    {
        return Compare(left, right, options) <= 0;
    }

    public static object GreaterThan(object? left, object? right, CsEvalOptions options)
    {
        return Compare(left, right, options) > 0;
    }

    public static object GreaterThanOrEqual(object? left, object? right, CsEvalOptions options)
    {
        return Compare(left, right, options) >= 0;
    }

    internal static int Compare(object? left, object? right, CsEvalOptions options)
    {
        if (left == null || right == null)
            throw new CsEvalException("Cannot compare null values");

        if (TypeHelpers.IsNumeric(left) && TypeHelpers.IsNumeric(right))
        {
            dynamic l = left, r = right;
            return l < r ? -1 : l > r ? 1 : 0;
        }

        return left switch
        {
            string ls when right is string rs => string.Compare(ls, rs, options.StringComparison),
            IComparable comparable => comparable.CompareTo(right),
            _ => throw new CsEvalException($"Cannot compare {left.GetType().Name} and {right.GetType().Name}")
        };
    }

    public static object? BitwiseAnd(object? left, object? right, CsEvalOptions options)
    {
        if (TypeHelpers.IsInteger(left) && TypeHelpers.IsInteger(right))
            return (dynamic)left! & (dynamic)right!;

        if (left is bool lb && right is bool rb)
            return lb & rb;

        throw new CsEvalException($"Cannot apply operator & to {left?.GetType().Name} and {right?.GetType().Name}");
    }

    public static object? BitwiseOr(object? left, object? right, CsEvalOptions options)
    {
        if (TypeHelpers.IsInteger(left) && TypeHelpers.IsInteger(right))
            return (dynamic)left! | (dynamic)right!;

        if (left is bool lb && right is bool rb)
            return lb | rb;

        throw new CsEvalException($"Cannot apply operator | to {left?.GetType().Name} and {right?.GetType().Name}");
    }

    public static object? BitwiseXor(object? left, object? right, CsEvalOptions options)
    {
        if (TypeHelpers.IsInteger(left) && TypeHelpers.IsInteger(right))
            return (dynamic)left! ^ (dynamic)right!;

        if (left is bool lb && right is bool rb)
            return lb ^ rb;

        throw new CsEvalException($"Cannot apply operator ^ to {left?.GetType().Name} and {right?.GetType().Name}");
    }

    public static object? BitwiseNot(object? value)
    {
        if (!TypeHelpers.IsNumeric(value))
            throw new CsEvalException($"Cannot apply bitwise NOT to {value?.GetType().Name ?? "null"}");

        return ~(dynamic)value!;
    }

    public static object? LeftShift(object? left, object? right)
    {
        if (!TypeHelpers.IsNumeric(left) || !TypeHelpers.IsNumeric(right))
            throw new CsEvalException($"Cannot apply left shift to {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");

        return (dynamic)left! << (int)(dynamic)right!;
    }

    public static object? RightShift(object? left, object? right)
    {
        if (!TypeHelpers.IsNumeric(left) || !TypeHelpers.IsNumeric(right))
            throw new CsEvalException($"Cannot apply right shift to {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");

        return (dynamic)left! >> (int)(dynamic)right!;
    }

    public static bool Contains(object? collection, object? value, CsEvalOptions options)
    {
        if (collection == null)
            throw new CsEvalException("Cannot check containment in null collection");

        if (collection is string str && value is string substr)
            return str.Contains(substr);

        if (collection is string strForChar && value is char ch)
            return strForChar.Contains(ch);

        if (collection is System.Collections.IEnumerable enumerable)
            return enumerable.Cast<object?>().Any(item => (bool)Equals(item, value, options));

        throw new CsEvalException($"Cannot use 'in' operator with {collection.GetType().Name}");
    }
}
