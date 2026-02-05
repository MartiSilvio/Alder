namespace CsEval.Runtime;

/// <summary>
/// Arithmetic, comparison, and bitwise operators.
/// Delegates to NumericDispatch for type-safe numeric operations.
/// </summary>
public static class Operators
{
    public static object? Negate(object? value)
    {
        // ECMA-334 §12.4.8: Lifted unary operators return null when operand is null
        if (value == null)
            return null;

        if (TypeHelpers.IsArithmetic(value))
            return NumericDispatch.Negate(value);

        throw new CsEvalException($"Cannot negate {value.GetType().Name}");
    }

    public static object? UnaryPlus(object? value)
    {
        // ECMA-334 §12.4.8: Lifted unary operators return null when operand is null
        if (value == null)
            return null;

        if (TypeHelpers.IsArithmetic(value))
            return NumericDispatch.UnaryPlus(value);

        throw new CsEvalException($"Operator '+' cannot be applied to operand of type '{value.GetType().Name}'");
    }

    public static object? Add(object? left, object? right, CsEvalOptions options) =>
        Add(left, right, options, null);

    public static object? Add(object? left, object? right, CsEvalOptions options, CsEvalContext? context)
    {
        if (left is string || right is string)
            return $"{left}{right}";

        if (left == null || right == null)
        {
            if (TypeHelpers.IsArithmetic(left) || TypeHelpers.IsArithmetic(right))
                return null; // Nullable arithmetic: num + null = null
            if (left == null && right == null)
                return null;
        }

        if (TypeHelpers.IsArithmetic(left) && TypeHelpers.IsArithmetic(right))
            return NumericDispatch.Add(left, right);

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

    public static object? Subtract(object? left, object? right)
    {
        if (left == null && right == null) return null;
        if (left == null || right == null)
        {
            if (TypeHelpers.IsArithmetic(left) || TypeHelpers.IsArithmetic(right))
                return null;
        }
        if ((left != null && !TypeHelpers.IsArithmetic(left)) || (right != null && !TypeHelpers.IsArithmetic(right)))
            throw new CsEvalException($"Operator '-' cannot be applied to operands of type '{left?.GetType().Name ?? "null"}' and '{right?.GetType().Name ?? "null"}'");
        return NumericDispatch.Subtract(left!, right!);
    }

    public static object? Multiply(object? left, object? right)
    {
        if (left == null && right == null) return null;
        if (left == null || right == null)
        {
            if (TypeHelpers.IsArithmetic(left) || TypeHelpers.IsArithmetic(right))
                return null;
        }
        if ((left != null && !TypeHelpers.IsArithmetic(left)) || (right != null && !TypeHelpers.IsArithmetic(right)))
            throw new CsEvalException($"Operator '*' cannot be applied to operands of type '{left?.GetType().Name ?? "null"}' and '{right?.GetType().Name ?? "null"}'");
        return NumericDispatch.Multiply(left!, right!);
    }

    public static object? Divide(object? left, object? right)
    {
        if (left == null && right == null) return null;
        if (left == null || right == null)
        {
            if (TypeHelpers.IsArithmetic(left) || TypeHelpers.IsArithmetic(right))
                return null;
        }
        if ((left != null && !TypeHelpers.IsArithmetic(left)) || (right != null && !TypeHelpers.IsArithmetic(right)))
            throw new CsEvalException($"Operator '/' cannot be applied to operands of type '{left?.GetType().Name ?? "null"}' and '{right?.GetType().Name ?? "null"}'");
        return NumericDispatch.Divide(left!, right!);
    }

    public static object? Modulo(object? left, object? right)
    {
        if (left == null && right == null) return null;
        if (left == null || right == null)
        {
            if (TypeHelpers.IsArithmetic(left) || TypeHelpers.IsArithmetic(right))
                return null;
        }
        if ((left != null && !TypeHelpers.IsArithmetic(left)) || (right != null && !TypeHelpers.IsArithmetic(right)))
            throw new CsEvalException($"Operator '%' cannot be applied to operands of type '{left?.GetType().Name ?? "null"}' and '{right?.GetType().Name ?? "null"}'");
        return NumericDispatch.Modulo(left!, right!);
    }

    public static new object Equals(object? left, object? right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;

        // IEEE 754: NaN is not equal to anything, including itself
        if (IsNaN(left) || IsNaN(right)) return false;

        if (left.Equals(right)) return true;

        if (TypeHelpers.IsArithmetic(left) && TypeHelpers.IsArithmetic(right))
            return NumericDispatch.Compare(left, right) == 0;

        return false;
    }

    public static object NotEquals(object? left, object? right)
    {
        if (left == null && right == null) return false;
        if (left == null || right == null) return true;

        // IEEE 754: NaN != anything is always true
        if (IsNaN(left) || IsNaN(right)) return true;

        return !(bool)Equals(left, right);
    }

    public static object LessThan(object? left, object? right, CsEvalOptions options)
    {
        if (left == null || right == null)
            return false;
        // IEEE 754: NaN comparisons always return false
        if (IsNaN(left) || IsNaN(right)) return false;
        return Compare(left, right, options) < 0;
    }

    public static object LessThanOrEqual(object? left, object? right, CsEvalOptions options)
    {
        if (left == null || right == null)
            return false;
        // IEEE 754: NaN comparisons always return false
        if (IsNaN(left) || IsNaN(right)) return false;
        return Compare(left, right, options) <= 0;
    }

    public static object GreaterThan(object? left, object? right, CsEvalOptions options)
    {
        if (left == null || right == null)
            return false;
        // IEEE 754: NaN comparisons always return false
        if (IsNaN(left) || IsNaN(right)) return false;
        return Compare(left, right, options) > 0;
    }

    public static object GreaterThanOrEqual(object? left, object? right, CsEvalOptions options)
    {
        if (left == null || right == null)
            return false;
        // IEEE 754: NaN comparisons always return false
        if (IsNaN(left) || IsNaN(right)) return false;
        return Compare(left, right, options) >= 0;
    }

    private static bool IsNaN(object? value) => value switch
    {
        double d => double.IsNaN(d),
        float f => float.IsNaN(f),
        _ => false
    };

    internal static int Compare(object? left, object? right, CsEvalOptions options)
    {
        if (TypeHelpers.IsArithmetic(left) && TypeHelpers.IsArithmetic(right))
            return NumericDispatch.Compare(left!, right!);

        return left switch
        {
            string ls when right is string rs => string.Compare(ls, rs, options.StringComparison),
            IComparable comparable => comparable.CompareTo(right),
            _ => throw new CsEvalException($"Cannot compare {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}")
        };
    }

    public static object? BitwiseAnd(object? left, object? right)
    {
        if (left is bool lb && right is bool rb)
            return lb & rb;

        // ECMA-334 §12.4.8: Lifted operators return null when either operand is null
        if (left == null || right == null)
        {
            if (TypeHelpers.IsInteger(left) || TypeHelpers.IsInteger(right) || left == null && right == null)
                return null;
        }

        if (TypeHelpers.IsInteger(left) && TypeHelpers.IsInteger(right))
            return NumericDispatch.BitwiseAnd(left!, right!);

        throw new CsEvalException($"Cannot apply operator & to {left?.GetType().Name} and {right?.GetType().Name}");
    }

    public static object? BitwiseOr(object? left, object? right)
    {
        if (left is bool lb && right is bool rb)
            return lb | rb;

        // ECMA-334 §12.4.8: Lifted operators return null when either operand is null
        if (left == null || right == null)
        {
            if (TypeHelpers.IsInteger(left) || TypeHelpers.IsInteger(right) || left == null && right == null)
                return null;
        }

        if (TypeHelpers.IsInteger(left) && TypeHelpers.IsInteger(right))
            return NumericDispatch.BitwiseOr(left!, right!);

        throw new CsEvalException($"Cannot apply operator | to {left?.GetType().Name} and {right?.GetType().Name}");
    }

    public static object? BitwiseXor(object? left, object? right)
    {
        if (left is bool lb && right is bool rb)
            return lb ^ rb;

        // ECMA-334 §12.4.8: Lifted operators return null when either operand is null
        if (left == null || right == null)
        {
            if (TypeHelpers.IsInteger(left) || TypeHelpers.IsInteger(right) || left == null && right == null)
                return null;
        }

        if (TypeHelpers.IsInteger(left) && TypeHelpers.IsInteger(right))
            return NumericDispatch.BitwiseXor(left!, right!);

        throw new CsEvalException($"Cannot apply operator ^ to {left?.GetType().Name} and {right?.GetType().Name}");
    }

    public static object? BitwiseNot(object? value)
    {
        // ECMA-334 §12.4.8: Lifted unary operators return null when operand is null
        if (value == null)
            return null;

        if (value is bool b)
            return !b;
        if (TypeHelpers.IsInteger(value))
            return NumericDispatch.BitwiseNot(value!);
        throw new CsEvalException($"Cannot apply bitwise NOT to {value?.GetType().Name ?? "null"}");
    }

    public static object? LeftShift(object? left, object? right)
    {
        // ECMA-334 §12.4.8: Lifted operators return null when either operand is null
        if (left == null || right == null)
            return null;

        if (!TypeHelpers.IsInteger(left) || !TypeHelpers.IsInteger(right))
            throw new CsEvalException($"Cannot apply left shift to {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");

        return NumericDispatch.LeftShift(left!, right!);
    }

    public static object? RightShift(object? left, object? right)
    {
        // ECMA-334 §12.4.8: Lifted operators return null when either operand is null
        if (left == null || right == null)
            return null;

        if (!TypeHelpers.IsInteger(left) || !TypeHelpers.IsInteger(right))
            throw new CsEvalException($"Cannot apply right shift to {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");

        return NumericDispatch.RightShift(left!, right!);
    }

    public static bool Contains(object? collection, object? value)
    {
        if (collection == null)
            throw new CsEvalException("Cannot check containment in null collection");

        if (collection is string str && value is string substr)
            return str.Contains(substr);

        if (collection is string strForChar && value is char ch)
            return strForChar.Contains(ch);

        if (collection is IEnumerable enumerable)
            return enumerable.Cast<object?>().Any(item => (bool)Equals(item, value));

        throw new CsEvalException($"Cannot use 'in' operator with {collection.GetType().Name}");
    }
}
