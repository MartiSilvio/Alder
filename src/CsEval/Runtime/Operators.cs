using CsEval.Diagnostics;
using CsEval.Parsing;

namespace CsEval.Runtime;

/// <summary>
/// Arithmetic, comparison, and bitwise operators.
/// Delegates to NumericDispatch for type-safe numeric operations.
/// </summary>
internal static class Operators
{
    internal enum LikePatternMode
    {
        Exact,
        Prefix,
        Suffix,
        Contains,
        General
    }

    public static object? Negate(object? value, bool isChecked = false)
    {
        // ECMA-334 §12.4.8: Lifted unary operators return null when operand is null
        if (value == null)
            return null;

        if (TypeHelpers.IsArithmetic(value))
            return NumericDispatch.Negate(value, isChecked);

        throw new CsEvalException(
            DiagnosticDescriptors.BadUnaryOp,
            TokenLexemes.GetCanonical(TokenType.Minus),
            value.GetType().Name);
    }

    public static object? UnaryPlus(object? value)
    {
        // ECMA-334 §12.4.8: Lifted unary operators return null when operand is null
        if (value == null)
            return null;

        if (TypeHelpers.IsArithmetic(value))
            return NumericDispatch.UnaryPlus(value);

        throw new CsEvalException(
            DiagnosticDescriptors.BadUnaryOp,
            TokenLexemes.GetCanonical(TokenType.Plus),
            value.GetType().Name);
    }

    public static object? LogicalNot(object? value)
    {
        // ECMA-334 §12.4.8: Lifted unary operators return null when operand is null
        if (value == null)
            return null;

        if (value is bool b)
            return !b;

        throw new CsEvalException(
            DiagnosticDescriptors.BadUnaryOp,
            TokenLexemes.GetCanonical(TokenType.Bang),
            value.GetType().Name);
    }

    public static object? Add(object? left, object? right, CsEvalOptions options) =>
        Add(left, right, options, null);

    public static object? Add(object? left, object? right, CsEvalOptions options, CsEvalContext? context, bool isChecked = false,
        bool isStringContext = false)
    {
        if (left is DateTime leftDate && right is TimeSpan rightSpan)
            return leftDate + rightSpan;

        if (left is TimeSpan leftSpan && right is DateTime rightDate)
            return rightDate + leftSpan;

        if (left is TimeSpan leftTimeSpan && right is TimeSpan rightTimeSpan)
            return leftTimeSpan + rightTimeSpan;

        if (left is string || right is string)
            return $"{left}{right}";

        if (left == null || right == null)
        {
            if (TypeHelpers.IsArithmetic(left) || TypeHelpers.IsArithmetic(right))
                return null; // Nullable arithmetic: num + null = null
            // §12.10.5: null + null in string context → empty string
            if (left == null && right == null)
                return isStringContext ? "" : null;
        }

        // §12.10.5: delegate combination — D + D → Delegate.Combine
        if (left is Delegate leftDel && right is Delegate rightDel)
            return Delegate.Combine(leftDel, rightDel);

        // ECMA-334 §12.10.5: E + int → E, int + E → E
        if (left != null && right != null && (left.GetType().IsEnum || right.GetType().IsEnum))
            return EnumArithmetic.Add(left, right);

        if (TypeHelpers.IsArithmetic(left) && TypeHelpers.IsArithmetic(right))
            return NumericDispatch.Add(left, right, isChecked);

        // Object merge via + operator (Extended mode only)
        if (options.LanguageMode == LanguageMode.Standard)
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.Plus),
                TypeNameFormatter.Of(left), TypeNameFormatter.Of(right));

        return Extensions.ObjectMergeOperator.MergeObjects(left, right, options, context);
    }

    public static object? Subtract(object? left, object? right, bool isChecked = false)
    {
        if (left is DateTime leftDate && right is DateTime rightDate)
            return leftDate - rightDate;

        if (left is DateTime date && right is TimeSpan span)
            return date - span;

        if (left is TimeSpan leftSpan && right is TimeSpan rightSpan)
            return leftSpan - rightSpan;

        // §12.10.6: delegate removal — D - D → Delegate.Remove
        if (left is Delegate leftDel && right is Delegate rightDel)
            return Delegate.Remove(leftDel, rightDel);

        // ECMA-334 §12.10.6: E - int → E, E - E → underlying
        if (left != null && right != null && (left.GetType().IsEnum || right.GetType().IsEnum))
            return EnumArithmetic.Subtract(left, right);

        return ApplyBinaryArithmetic(
            left,
            right,
            TokenLexemes.GetCanonical(TokenType.Minus),
            (l, r) => NumericDispatch.Subtract(l, r, isChecked));
    }

    public static object? Multiply(object? left, object? right) =>
        Multiply(left, right, null);

    public static object? Multiply(object? left, object? right, CsEvalOptions? options, bool isChecked = false)
    {
        if (left is string || right is string)
        {
            if (options?.LanguageMode == LanguageMode.Extended)
                return StringMultiply(left, right);
            // In Standard mode, string * anything falls through to arithmetic and throws
        }
        return ApplyBinaryArithmetic(
            left,
            right,
            TokenLexemes.GetCanonical(TokenType.Star),
            (l, r) => NumericDispatch.Multiply(l, r, isChecked));
    }

    public static object? Divide(object? left, object? right) =>
        ApplyBinaryArithmetic(left, right, TokenLexemes.GetCanonical(TokenType.Slash), NumericDispatch.Divide);

    public static object? Modulo(object? left, object? right) =>
        ApplyBinaryArithmetic(left, right, TokenLexemes.GetCanonical(TokenType.Percent), NumericDispatch.Modulo);

    public static object? Power(object? left, object? right)
    {
        if (left == null || right == null)
        {
            if (TypeHelpers.IsArithmetic(left) || TypeHelpers.IsArithmetic(right))
                return null; // Nullable arithmetic
            if (left == null && right == null)
                return null;
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.StarStar),
                TypeNameFormatter.Of(left),
                TypeNameFormatter.Of(right));
        }

        if (!TypeHelpers.IsArithmetic(left) || !TypeHelpers.IsArithmetic(right))
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.StarStar),
                left.GetType().Name,
                right.GetType().Name);

        var l = Convert.ToDouble(left);
        var r = Convert.ToDouble(right);
        return Math.Pow(l, r);
    }

    private static object? ApplyBinaryArithmetic(object? left, object? right, string op, Func<object, object, object?> dispatch)
    {
        if (left == null && right == null) return null;
        if (left == null || right == null)
        {
            if (TypeHelpers.IsArithmetic(left) || TypeHelpers.IsArithmetic(right))
                return null;
        }
        if ((left != null && !TypeHelpers.IsArithmetic(left)) || (right != null && !TypeHelpers.IsArithmetic(right)))
            throw new CsEvalException(DiagnosticDescriptors.BadBinaryOps, op, TypeNameFormatter.Of(left), TypeNameFormatter.Of(right));
        return dispatch(left!, right!);
    }

    public new static object Equals(object? left, object? right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;

        // IEEE 754: NaN is not equal to anything, including itself
        if (IsNaN(left) || IsNaN(right)) return false;

        // ECMA-334 §12.12.11: Tuple equality operators - element-wise comparison with type promotion.
        // Must be checked BEFORE Object.Equals because ValueTuple<int,long>.Equals(ValueTuple<long,int>)
        // returns false even when elements are semantically equal.
        if (left is System.Runtime.CompilerServices.ITuple leftTuple &&
            right is System.Runtime.CompilerServices.ITuple rightTuple)
        {
            if (leftTuple.Length != rightTuple.Length)
                return false;

            for (var i = 0; i < leftTuple.Length; i++)
            {
                if (!(bool)Equals(leftTuple[i], rightTuple[i]))
                    return false;
            }

            return true;
        }

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

    public static object StrictEquals(object? left, object? right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;
        if (IsNaN(left) || IsNaN(right)) return false;
        if (left.GetType() != right.GetType()) return false;
        return left.Equals(right);
    }

    public static object StrictNotEquals(object? left, object? right)
    {
        return !(bool)StrictEquals(left, right);
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
            _ => throw new CsEvalException(DiagnosticDescriptors.BadBinaryOps, "<>", TypeNameFormatter.Of(left), TypeNameFormatter.Of(right))
        };
    }

    public static object? BitwiseAnd(object? left, object? right)
    {
        if (left is bool lb && right is bool rb)
            return lb & rb;

        // ECMA-334 §12.13.5: Three-value bool? logic for &
        // Only applies when both operands are bool/null (i.e., bool? & bool?)
        if (left is bool or null && right is bool or null)
        {
            if (left is false || right is false)
                return false;
            return null;
        }

        // ECMA-334 §12.4.8: Lifted integer operators return null when either operand is null
        if (left == null || right == null)
        {
            if (IsIntegerOrChar(left) || IsIntegerOrChar(right))
                return null;
        }

        // ECMA-334 §12.13: Bitwise operators apply to integer types and char
        // ECMA-334 §12.13.3: E & E → E
        if (left != null && right != null && left.GetType().IsEnum && right.GetType().IsEnum)
            return EnumArithmetic.BitwiseOp(left, right, NumericDispatch.BitwiseAnd);

        if (IsIntegerOrChar(left) && IsIntegerOrChar(right))
            return NumericDispatch.BitwiseAnd(left!, right!);

        throw new CsEvalException(
            DiagnosticDescriptors.BadBinaryOps,
            TokenLexemes.GetCanonical(TokenType.Amp),
            TypeNameFormatter.Of(left),
            TypeNameFormatter.Of(right));
    }

    public static object? BitwiseOr(object? left, object? right)
    {
        if (left is bool lb && right is bool rb)
            return lb | rb;

        // ECMA-334 §12.13.5: Three-value bool? logic for |
        // Only applies when both operands are bool/null (i.e., bool? | bool?)
        if (left is bool or null && right is bool or null)
        {
            if (left is true || right is true)
                return true;
            return null;
        }

        // ECMA-334 §12.4.8: Lifted integer operators return null when either operand is null
        if (left == null || right == null)
        {
            if (IsIntegerOrChar(left) || IsIntegerOrChar(right))
                return null;
        }

        // ECMA-334 §12.13.3: E | E → E
        if (left != null && right != null && left.GetType().IsEnum && right.GetType().IsEnum)
            return EnumArithmetic.BitwiseOp(left, right, NumericDispatch.BitwiseOr);

        if (IsIntegerOrChar(left) && IsIntegerOrChar(right))
            return NumericDispatch.BitwiseOr(left!, right!);

        throw new CsEvalException(
            DiagnosticDescriptors.BadBinaryOps,
            TokenLexemes.GetCanonical(TokenType.Pipe),
            TypeNameFormatter.Of(left),
            TypeNameFormatter.Of(right));
    }

    public static object? BitwiseXor(object? left, object? right)
    {
        if (left is bool lb && right is bool rb)
            return lb ^ rb;

        // ECMA-334 §12.13.5: Three-value bool? logic for ^
        // Only applies when both operands are bool/null (i.e., bool? ^ bool?)
        if (left is bool or null && right is bool or null && (left == null || right == null))
            return null;

        // ECMA-334 §12.4.8: Lifted integer operators return null when either operand is null
        if (left == null || right == null)
        {
            if (IsIntegerOrChar(left) || IsIntegerOrChar(right) || left == null && right == null)
                return null;
        }

        // ECMA-334 §12.13.3: E ^ E → E
        if (left != null && right != null && left.GetType().IsEnum && right.GetType().IsEnum)
            return EnumArithmetic.BitwiseOp(left, right, NumericDispatch.BitwiseXor);

        if (IsIntegerOrChar(left) && IsIntegerOrChar(right))
            return NumericDispatch.BitwiseXor(left!, right!);

        throw new CsEvalException(
            DiagnosticDescriptors.BadBinaryOps,
            TokenLexemes.GetCanonical(TokenType.Caret),
            TypeNameFormatter.Of(left),
            TypeNameFormatter.Of(right));
    }

    public static object? BitwiseNot(object? value)
    {
        // ECMA-334 §12.4.8: Lifted unary operators return null when operand is null
        if (value == null)
            return null;

        if (value is bool b)
            return !b;
        // ECMA-334 §12.13.3: ~E → E
        if (value.GetType().IsEnum)
            return EnumArithmetic.BitwiseNot(value);
        // ECMA-334 §12.9.5: ~ is defined for integer types and char
        if (TypeHelpers.IsInteger(value) || value is char)
            return NumericDispatch.BitwiseNot(value!);
        throw new CsEvalException(
            DiagnosticDescriptors.BadUnaryOp,
            TokenLexemes.GetCanonical(TokenType.Tilde),
            TypeNameFormatter.Of(value));
    }

    public static object? LeftShift(object? left, object? right)
    {
        // ECMA-334 §12.4.8: Lifted operators return null when either operand is null
        if (left == null || right == null)
            return null;

        // ECMA-334 §12.11: Shift operators accept integer types and char
        if (!IsIntegerOrChar(left) || !TypeHelpers.IsInteger(right))
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.LessLess),
                TypeNameFormatter.Of(left),
                TypeNameFormatter.Of(right));

        return NumericDispatch.LeftShift(left!, right!);
    }

    public static object? RightShift(object? left, object? right)
    {
        // ECMA-334 §12.4.8: Lifted operators return null when either operand is null
        if (left == null || right == null)
            return null;

        if (!IsIntegerOrChar(left) || !TypeHelpers.IsInteger(right))
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.GreaterGreater),
                TypeNameFormatter.Of(left),
                TypeNameFormatter.Of(right));

        return NumericDispatch.RightShift(left!, right!);
    }

    public static object? UnsignedRightShift(object? left, object? right)
    {
        if (left == null || right == null)
            return null;

        if (!IsIntegerOrChar(left) || !TypeHelpers.IsInteger(right))
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.GreaterGreaterGreater),
                TypeNameFormatter.Of(left),
                TypeNameFormatter.Of(right));

        return NumericDispatch.UnsignedRightShift(left!, right!);
    }

    private static bool IsIntegerOrChar(object? value) =>
        TypeHelpers.IsInteger(value) || value is char;

    public static bool Contains(object? collection, object? value)
    {
        if (collection == null)
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.In),
                TypeNameFormatter.Of(value),
                TypeNameFormatter.Null);

        if (collection is string str && value is string substr)
            return str.Contains(substr);

        if (collection is string strForChar && value is char ch)
            return strForChar.Contains(ch);

        // Prefer collection-native membership when available.
        // This avoids LINQ iterator allocations and lets types like List<T>
        // use their optimized Contains implementations.
        if (collection is IList list)
            return list.Contains(value);

        if (collection is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if ((bool)Equals(item, value))
                    return true;
            }

            return false;
        }

        throw new CsEvalException(
            DiagnosticDescriptors.BadBinaryOps,
            TokenLexemes.GetCanonical(TokenType.In),
            TypeNameFormatter.Of(value),
            collection.GetType().Name);
    }

    public static bool InOperator(object? value, object? collection)
    {
        return Contains(collection, value);
    }

    internal static LikePatternMode ClassifyLikePattern(string pattern)
    {
        if (pattern.Length == 0)
            return LikePatternMode.Exact;

        var firstPercent = pattern.IndexOf('%');
        var firstUnderscore = pattern.IndexOf('_');

        if (firstPercent < 0 && firstUnderscore < 0)
            return LikePatternMode.Exact;

        if (firstUnderscore >= 0)
            return LikePatternMode.General;

        if (firstPercent == pattern.Length - 1 && pattern.LastIndexOf('%') == firstPercent)
            return LikePatternMode.Prefix;

        if (firstPercent == 0 && pattern.LastIndexOf('%') == 0)
            return LikePatternMode.Suffix;

        if (pattern is ['%', _, ..] &&
            pattern[^1] == '%' &&
            pattern.IndexOf('%', 1) == pattern.Length - 1)
        {
            return LikePatternMode.Contains;
        }

        return LikePatternMode.General;
    }

    public static object? StringMultiply(object? left, object? right)
    {
        string? str;
        object? countObj;

        if (left is string s)
        {
            str = s;
            countObj = right;
        }
        else if (right is string s2)
        {
            str = s2;
            countObj = left;
        }
        else
        {
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.Star),
                TypeNameFormatter.Of(left), TypeNameFormatter.Of(right));
        }

        if (countObj == null)
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.Star),
                TypeNameFormatter.Of(left), TypeNameFormatter.Of(right));

        if (!TypeHelpers.IsInteger(countObj))
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.Star),
                TypeNameFormatter.Of(left), TypeNameFormatter.Of(right));

        int count = Convert.ToInt32(countObj);
        if (count < 0)
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.Star),
                TypeNameFormatter.Of(left), TypeNameFormatter.Of(right));
        if (count == 0)
            return "";

        return string.Concat(Enumerable.Repeat(str, count));
    }

    public static bool Like(object? left, object? right, CsEvalOptions? options = null)
    {
        if (left is not string str || right is not string pattern)
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.Like),
                TypeNameFormatter.Of(left),
                TypeNameFormatter.Of(right));

        if (pattern.Length == 0)
            return str.Length == 0;

        var comparison = options?.StringComparison ?? StringComparison.Ordinal;

        return ClassifyLikePattern(pattern) switch
        {
            LikePatternMode.Exact =>
                string.Equals(str, pattern, comparison),
            LikePatternMode.Prefix =>
                str.StartsWith(pattern.Substring(0, pattern.Length - 1), comparison),
            LikePatternMode.Suffix =>
                str.EndsWith(pattern.Substring(1), comparison),
            LikePatternMode.Contains =>
                str.IndexOf(pattern.Substring(1, pattern.Length - 2), comparison) >= 0,
            _ => LikeMatchesPattern(str.AsSpan(), pattern.AsSpan(), comparison)
        };
    }

    private static bool LikeMatchesPattern(ReadOnlySpan<char> value, ReadOnlySpan<char> pattern, StringComparison comparison)
    {
        var ignoreCase = comparison == StringComparison.OrdinalIgnoreCase;
        var valueIndex = 0;
        var patternIndex = 0;
        var lastPercentIndex = -1;
        var fallbackValueIndex = 0;

        while (valueIndex < value.Length)
        {
            if (patternIndex < pattern.Length &&
                (pattern[patternIndex] == '_' || CharEquals(pattern[patternIndex], value[valueIndex], ignoreCase)))
            {
                patternIndex++;
                valueIndex++;
                continue;
            }

            if (patternIndex < pattern.Length && pattern[patternIndex] == '%')
            {
                lastPercentIndex = patternIndex++;
                fallbackValueIndex = valueIndex;
                continue;
            }

            if (lastPercentIndex >= 0)
            {
                patternIndex = lastPercentIndex + 1;
                valueIndex = ++fallbackValueIndex;
                continue;
            }

            return false;
        }

        while (patternIndex < pattern.Length && pattern[patternIndex] == '%')
            patternIndex++;

        return patternIndex == pattern.Length;
    }

    private static bool CharEquals(char a, char b, bool ignoreCase)
        => ignoreCase ? char.ToUpperInvariant(a) == char.ToUpperInvariant(b) : a == b;

    public static object RegexMatch(object? left, object? right)
    {
        return Extensions.RegexMatchOperator.IsMatch(left, right);
    }

    public static object RegexNotMatch(object? left, object? right)
    {
        return Extensions.RegexMatchOperator.IsNotMatch(left, right);
    }

    public static object Spaceship(object? left, object? right)
    {
        return Extensions.SpaceshipOperator.Compare(left, right);
    }
}
