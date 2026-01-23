using System.Reflection;

namespace CsEval.Evaluation;

/// <summary>
/// Static helper methods called by compiled expressions.
/// These mirror the behavior in Evaluator.Operators.cs for consistency.
/// </summary>
public static class CompilerHelpers
{
    public static bool IsTruthy(object? value)
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

    public static object? Negate(object? value)
    {
        return value switch
        {
            int i => -i,
            long l => -l,
            double d => -d,
            float f => -f,
            decimal m => -m,
            _ => throw new EvalException($"Cannot negate {value?.GetType().Name ?? "null"}")
        };
    }

    public static object? Add(object? left, object? right, CsEvalOptions options)
    {
        // String concatenation
        if (left is string || right is string)
            return $"{left}{right}";

        // Numeric addition only for compiled expressions (no object merging)
        if (IsNumeric(left) && IsNumeric(right))
        {
            if (left is decimal || right is decimal)
                return ToDecimal(left) + ToDecimal(right);
            if (left is double or float || right is double or float)
                return ToDouble(left) + ToDouble(right);
            if (BothFitInInt(left, right))
                return ToInt(left) + ToInt(right);
            return ToLong(left) + ToLong(right);
        }

        // Object merging not supported in compiled expressions
        // The CanCompile check should prevent this path, but throw just in case
        throw new EvalException(
            $"Cannot add {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"} in compiled expression. " +
            "Object merging requires tree-walking evaluation.");
    }

    public static object? Subtract(object? left, object? right, CsEvalOptions options)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            if (left is decimal || right is decimal)
                return ToDecimal(left) - ToDecimal(right);
            if (left is double or float || right is double or float)
                return ToDouble(left) - ToDouble(right);
            if (BothFitInInt(left, right))
                return ToInt(left) - ToInt(right);
            return ToLong(left) - ToLong(right);
        }

        throw new EvalException($"Cannot subtract {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    public static object? Multiply(object? left, object? right, CsEvalOptions options)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            if (left is decimal || right is decimal)
                return ToDecimal(left) * ToDecimal(right);
            if (left is double or float || right is double or float)
                return ToDouble(left) * ToDouble(right);
            if (BothFitInInt(left, right))
                return ToInt(left) * ToInt(right);
            return ToLong(left) * ToLong(right);
        }

        throw new EvalException($"Cannot multiply {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    public static object? Divide(object? left, object? right, CsEvalOptions options)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            if (left is decimal || right is decimal)
            {
                var r = ToDecimal(right);
                if (r == 0) throw new EvalException("Division by zero");
                return ToDecimal(left) / r;
            }

            var rd = ToDouble(right);
            if (rd == 0) throw new EvalException("Division by zero");
            return ToDouble(left) / rd;
        }

        throw new EvalException($"Cannot divide {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    public static object? Modulo(object? left, object? right, CsEvalOptions options)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            if (left is decimal || right is decimal)
            {
                var r = ToDecimal(right);
                if (r == 0) throw new EvalException("Modulo by zero");
                return ToDecimal(left) % r;
            }

            if (left is double or float || right is double or float)
            {
                var r = ToDouble(right);
                if (r == 0) throw new EvalException("Modulo by zero");
                return ToDouble(left) % r;
            }

            if (BothFitInInt(left, right))
            {
                var ri = ToInt(right);
                if (ri == 0) throw new EvalException("Modulo by zero");
                return ToInt(left) % ri;
            }

            var rl = ToLong(right);
            if (rl == 0) throw new EvalException("Modulo by zero");
            return ToLong(left) % rl;
        }

        throw new EvalException($"Cannot modulo {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    public static object Equals(object? left, object? right, CsEvalOptions options)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;
        if (left.Equals(right)) return true;

        if (IsNumeric(left) && IsNumeric(right))
            return ToDouble(left) == ToDouble(right);

        return false;
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

    private static int Compare(object? left, object? right, CsEvalOptions options)
    {
        if (left == null || right == null)
            throw new EvalException("Cannot compare null values");

        if (IsNumeric(left) && IsNumeric(right))
            return ToDouble(left).CompareTo(ToDouble(right));

        return left switch
        {
            string ls when right is string rs => string.Compare(ls, rs, options.StringComparison),
            IComparable comparable => comparable.CompareTo(right),
            _ => throw new EvalException($"Cannot compare {left.GetType().Name} and {right.GetType().Name}")
        };
    }

    public static object? GetMember(object? obj, string name, CsEvalOptions options, bool nullSafe)
    {
        if (nullSafe && obj == null)
            return null;

        if (obj == null)
            throw new EvalException($"Cannot access property '{name}' on null");

        if (options.Security.SafeMode && !options.Security.AllowPropertyRead)
            throw new EvalException($"Property access blocked in SafeMode: {name}");

        if (obj is IDictionary<string, object?> dict)
        {
            if (dict.TryGetValue(name, out var value))
                return value;

            if (options.IgnoreCase)
            {
                foreach (var key in dict.Keys)
                {
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                        return dict[key];
                }
            }

            throw new EvalException($"Property '{name}' not found");
        }

        var type = obj.GetType();
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (options.IgnoreCase)
            bindingFlags |= BindingFlags.IgnoreCase;

        var prop = TypeCache.GetProperty(type, name, bindingFlags);
        if (prop != null)
            return TypeCache.GetPropertyValue(prop, obj);

        var field = TypeCache.GetField(type, name, bindingFlags);
        if (field != null)
            return field.GetValue(obj);

        throw new EvalException($"Property '{name}' not found on type '{type.Name}'");
    }

    private static bool IsNumeric(object? value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;

    private static double ToDouble(object? value) => Convert.ToDouble(value);
    private static decimal ToDecimal(object? value) => Convert.ToDecimal(value);
    private static long ToLong(object? value) => Convert.ToInt64(value);
    private static int ToInt(object? value) => Convert.ToInt32(value);

    private static bool BothFitInInt(object? left, object? right) =>
        left is int or short or ushort or byte or sbyte &&
        right is int or short or ushort or byte or sbyte;
}
