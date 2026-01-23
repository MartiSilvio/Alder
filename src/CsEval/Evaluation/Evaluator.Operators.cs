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

        if (IsNumeric(left) && IsNumeric(right))
            return ToDouble(left) == ToDouble(right);

        return false;
    }

    private int Compare(object? left, object? right)
    {
        if (left == null || right == null)
            throw new EvalException("Cannot compare null values");

        if (IsNumeric(left) && IsNumeric(right))
            return ToDouble(left).CompareTo(ToDouble(right));

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

        if (IsNumeric(left) && IsNumeric(right))
        {
            if (left is decimal || right is decimal)
                return ToDecimal(left) + ToDecimal(right);
            if (left is double or float || right is double or float)
                return ToDouble(left) + ToDouble(right);
            // C# behavior: small integers promote to int, int + int → int
            if (BothFitInInt(left, right))
                return ToInt(left) + ToInt(right);
            return ToLong(left) + ToLong(right);
        }

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

            // Copy properties from the left object via reflection
            var leftType = left.GetType();
            foreach (var prop in TypeCache.GetProperties(leftType, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (prop.CanRead)
                    merged[prop.Name] = prop.GetValue(left);
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

            // Override/add properties from the right object via reflection
            var rightType = right.GetType();
            foreach (var prop in TypeCache.GetProperties(rightType, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (prop.CanRead)
                    merged[prop.Name] = prop.GetValue(right);
            }

            return merged;
        }

        // Both are typed objects - merge by reflecting both
        if (left != null && right != null)
        {
            var comparer = _options.StringComparer;
            var merged = new Dictionary<string, object?>(comparer);

            // Copy properties from the left object via reflection
            var leftType = left.GetType();
            foreach (var prop in TypeCache.GetProperties(leftType, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (prop.CanRead)
                    merged[prop.Name] = prop.GetValue(left);
            }

            // Override/add properties from the right object via reflection
            var rightType = right.GetType();
            foreach (var prop in TypeCache.GetProperties(rightType, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (prop.CanRead)
                    merged[prop.Name] = prop.GetValue(right);
            }

            return merged;
        }

        throw new EvalException($"Cannot add {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? Subtract(object? left, object? right)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            if (left is decimal || right is decimal)
                return ToDecimal(left) - ToDecimal(right);
            if (left is double or float || right is double or float)
                return ToDouble(left) - ToDouble(right);
            // C# behavior: small integers promote to int, int - int → int
            if (BothFitInInt(left, right))
                return ToInt(left) - ToInt(right);
            return ToLong(left) - ToLong(right);
        }

        throw new EvalException($"Cannot subtract {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? Multiply(object? left, object? right)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            if (left is decimal || right is decimal)
                return ToDecimal(left) * ToDecimal(right);
            if (left is double or float || right is double or float)
                return ToDouble(left) * ToDouble(right);
            // C# behavior: small integers promote to int, int * int → int
            if (BothFitInInt(left, right))
                return ToInt(left) * ToInt(right);
            return ToLong(left) * ToLong(right);
        }

        throw new EvalException($"Cannot multiply {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? Divide(object? left, object? right)
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

    private static object? Modulo(object? left, object? right)
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
            // C# behavior: small integers promote to int
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

    private static object? Negate(object? value)
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

    private static object? BitwiseNot(object? value)
    {
        if (IsNumeric(value))
            return ~ToLong(value);
        throw new EvalException($"Cannot apply bitwise NOT to {value?.GetType().Name ?? "null"}");
    }

    private static object? BitwiseAnd(object? left, object? right)
    {
        if (IsNumeric(left) && IsNumeric(right))
            return ToLong(left) & ToLong(right);
        throw new EvalException($"Cannot apply bitwise AND to {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? BitwiseOr(object? left, object? right)
    {
        if (IsNumeric(left) && IsNumeric(right))
            return ToLong(left) | ToLong(right);
        throw new EvalException($"Cannot apply bitwise OR to {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? BitwiseXor(object? left, object? right)
    {
        if (IsNumeric(left) && IsNumeric(right))
            return ToLong(left) ^ ToLong(right);
        throw new EvalException($"Cannot apply bitwise XOR to {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? LeftShift(object? left, object? right)
    {
        if (IsNumeric(left) && IsNumeric(right))
            return ToLong(left) << (int)ToLong(right);
        throw new EvalException($"Cannot apply left shift to {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? RightShift(object? left, object? right)
    {
        if (IsNumeric(left) && IsNumeric(right))
            return ToLong(left) >> (int)ToLong(right);
        throw new EvalException($"Cannot apply right shift to {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static bool IsIntegral(object? value) =>
        value is int or long or short or byte or sbyte or uint or ulong or ushort;

    private static bool IsNumeric(object? value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;

    private static bool IsFloatingPoint(object? value) =>
        value is double or float or decimal;

    private static double ToDouble(object? value) => Convert.ToDouble(value);
    private static decimal ToDecimal(object? value) => Convert.ToDecimal(value);
    private static long ToLong(object? value) => Convert.ToInt64(value);
    private static int ToInt(object? value) => Convert.ToInt32(value);

    /// <summary>
    /// C# promotes small integers (byte, sbyte, short, ushort) to int for arithmetic.
    /// Returns true if both operands are int or smaller signed/unsigned types.
    /// </summary>
    private static bool BothFitInInt(object? left, object? right) =>
        left is int or short or ushort or byte or sbyte &&
        right is int or short or ushort or byte or sbyte;
}
