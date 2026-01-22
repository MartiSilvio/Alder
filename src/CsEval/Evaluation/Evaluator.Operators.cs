namespace CsEval.Evaluation;

public sealed partial class Evaluator
{
    private static bool IsTruthy(object? value)
    {
        if (value == null) return false;
        if (value is bool b) return b;
        if (value is int i) return i != 0;
        if (value is long l) return l != 0;
        if (value is double d) return d != 0;
        if (value is string s) return !string.IsNullOrEmpty(s);
        return true;
    }

    private static new bool Equals(object? left, object? right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;
        if (left.Equals(right)) return true;

        if (IsNumeric(left) && IsNumeric(right))
            return ToDouble(left) == ToDouble(right);

        return false;
    }

    private static int Compare(object? left, object? right)
    {
        if (left == null || right == null)
            throw new EvalException("Cannot compare null values");

        if (IsNumeric(left) && IsNumeric(right))
            return ToDouble(left).CompareTo(ToDouble(right));

        if (left is string ls && right is string rs)
            return string.Compare(ls, rs, StringComparison.Ordinal);

        if (left is IComparable comparable)
            return comparable.CompareTo(right);

        throw new EvalException($"Cannot compare {left.GetType().Name} and {right.GetType().Name}");
    }

    private static object? Add(object? left, object? right)
    {
        if (left is string || right is string)
            return $"{left}{right}";

        if (IsNumeric(left) && IsNumeric(right))
        {
            if (left is double || right is double)
                return ToDouble(left) + ToDouble(right);
            return ToLong(left) + ToLong(right);
        }

        throw new EvalException($"Cannot add {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? Subtract(object? left, object? right)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            if (left is double || right is double)
                return ToDouble(left) - ToDouble(right);
            return ToLong(left) - ToLong(right);
        }

        throw new EvalException($"Cannot subtract {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? Multiply(object? left, object? right)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            if (left is double || right is double)
                return ToDouble(left) * ToDouble(right);
            return ToLong(left) * ToLong(right);
        }

        throw new EvalException($"Cannot multiply {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? Divide(object? left, object? right)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            var r = ToDouble(right);
            if (r == 0) throw new EvalException("Division by zero");
            return ToDouble(left) / r;
        }

        throw new EvalException($"Cannot divide {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? Modulo(object? left, object? right)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            var r = ToLong(right);
            if (r == 0) throw new EvalException("Modulo by zero");
            return ToLong(left) % r;
        }

        throw new EvalException($"Cannot modulo {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? Negate(object? value)
    {
        if (value is int i) return -i;
        if (value is long l) return -l;
        if (value is double d) return -d;
        throw new EvalException($"Cannot negate {value?.GetType().Name ?? "null"}");
    }

    private static bool IsNumeric(object? value) =>
        value is int or long or double or float or decimal or short or byte;

    private static double ToDouble(object? value) => Convert.ToDouble(value);
    private static long ToLong(object? value) => Convert.ToInt64(value);
}
