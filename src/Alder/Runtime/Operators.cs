using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder.Runtime;

/// <summary>
/// Arithmetic, comparison, and bitwise operators.
/// Delegates to NumericDispatch for type-safe numeric operations.
/// </summary>
internal static class Operators
{
    private static readonly ConcurrentDictionary<(Type Left, Type Right, string Op), MethodInfo?> UserDefinedBinaryOperatorCache = new();

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

        throw new AlderException(
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

        throw new AlderException(
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
            return b ? BoxedConstants.False : BoxedConstants.True;

        throw new AlderException(
            DiagnosticDescriptors.BadUnaryOp,
            TokenLexemes.GetCanonical(TokenType.Bang),
            value.GetType().Name);
    }

    public static object? Add(object? left, object? right, AlderConfig config) =>
        Add(left, right, config, null);

    public static object? Add(object? left, object? right, AlderConfig config, AlderContext? context, bool isChecked = false,
        bool isStringContext = false)
    {
        if (left is DateTime leftDate && right is TimeSpan rightSpan)
            return leftDate + rightSpan;

        if (left is TimeSpan leftSpan && right is DateTime rightDate)
            return rightDate + leftSpan;

        if (left is TimeSpan leftTimeSpan && right is TimeSpan rightTimeSpan)
            return leftTimeSpan + rightTimeSpan;

        if (left is string || right is string)
            return string.Concat(left?.ToString(), right?.ToString());

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

        if (left != null && right != null &&
            TryInvokeUserDefinedBinaryOperator(left, right, "op_Addition", out var userResult, isChecked))
            return userResult;

        // Object merge via + operator (Extended mode only)
        if (config.LanguageMode == LanguageMode.Standard)
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.Plus),
                TypeNameFormatter.Of(left), TypeNameFormatter.Of(right));

        return Extensions.ObjectMergeOperator.MergeObjects(left, right, config.Comparer, context);
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
            (l, r) => NumericDispatch.Subtract(l, r, isChecked),
            "op_Subtraction", isChecked);
    }

    public static object? Multiply(object? left, object? right, LanguageMode languageMode, bool isChecked = false)
    {
        if (left is string || right is string)
        {
            if (languageMode == LanguageMode.Extended)
                return StringMultiply(left, right);
        }
        return ApplyBinaryArithmetic(
            left,
            right,
            TokenLexemes.GetCanonical(TokenType.Star),
            (l, r) => NumericDispatch.Multiply(l, r, isChecked),
            "op_Multiply", isChecked);
    }

    public static object? Divide(object? left, object? right) =>
        ApplyBinaryArithmetic(left, right, TokenLexemes.GetCanonical(TokenType.Slash), NumericDispatch.Divide, "op_Division");

    public static object? Modulo(object? left, object? right) =>
        ApplyBinaryArithmetic(left, right, TokenLexemes.GetCanonical(TokenType.Percent), NumericDispatch.Modulo, "op_Modulus");

    public static object? Power(object? left, object? right)
    {
        if (left == null || right == null)
        {
            if (TypeHelpers.IsArithmetic(left) || TypeHelpers.IsArithmetic(right))
                return null; // Nullable arithmetic
            if (left == null && right == null)
                return null;
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.StarStar),
                TypeNameFormatter.Of(left),
                TypeNameFormatter.Of(right));
        }

        if (!TypeHelpers.IsArithmetic(left) || !TypeHelpers.IsArithmetic(right))
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.StarStar),
                left.GetType().Name,
                right.GetType().Name);

        var l = Convert.ToDouble(left);
        var r = Convert.ToDouble(right);
        return Math.Pow(l, r);
    }

    private static object? ApplyBinaryArithmetic(
        object? left, object? right, string op, Func<object, object, object?> dispatch,
        string? userDefinedOperatorName = null, bool isChecked = false)
    {
        if (left == null && right == null) return null;
        if (left == null || right == null)
        {
            if (TypeHelpers.IsArithmetic(left) || TypeHelpers.IsArithmetic(right))
                return null;
        }

        if (left != null && right != null && TypeHelpers.IsArithmetic(left) && TypeHelpers.IsArithmetic(right))
            return dispatch(left, right);

        if (left != null && right != null && userDefinedOperatorName != null &&
            TryInvokeUserDefinedBinaryOperator(left, right, userDefinedOperatorName, out var result, isChecked))
            return result;

        throw new AlderException(DiagnosticDescriptors.BadBinaryOps, op, TypeNameFormatter.Of(left), TypeNameFormatter.Of(right));
    }

    public new static object Equals(object? left, object? right)
    {
        if (left == null && right == null) return BoxedConstants.True;
        if (left == null || right == null) return BoxedConstants.False;

        // IEEE 754: NaN is not equal to anything, including itself
        if (IsNaN(left) || IsNaN(right)) return BoxedConstants.False;

        // ECMA-334 §12.12.11: Tuple equality operators - element-wise comparison with type promotion.
        // Must be checked BEFORE Object.Equals because ValueTuple<int,long>.Equals(ValueTuple<long,int>)
        // returns false even when elements are semantically equal.
        if (left is System.Runtime.CompilerServices.ITuple leftTuple &&
            right is System.Runtime.CompilerServices.ITuple rightTuple)
        {
            if (leftTuple.Length != rightTuple.Length)
                return BoxedConstants.False;

            for (var i = 0; i < leftTuple.Length; i++)
            {
                if (!(bool)Equals(leftTuple[i], rightTuple[i]))
                    return BoxedConstants.False;
            }

            return BoxedConstants.True;
        }

        if (left.Equals(right)) return BoxedConstants.True;

        if (TypeHelpers.IsArithmetic(left) && TypeHelpers.IsArithmetic(right))
            return NumericDispatch.Compare(left, right) == 0 ? BoxedConstants.True : BoxedConstants.False;

        if (TryInvokeUserDefinedBinaryOperator(left, right, "op_Equality", out var userResult))
            return userResult ?? BoxedConstants.False;

        return BoxedConstants.False;
    }

    public static object NotEquals(object? left, object? right)
    {
        if (left == null && right == null) return BoxedConstants.False;
        if (left == null || right == null) return BoxedConstants.True;

        // IEEE 754: NaN != anything is always true
        if (IsNaN(left) || IsNaN(right)) return BoxedConstants.True;

        return !(bool)Equals(left, right) ? BoxedConstants.True : BoxedConstants.False;
    }

    public static object StrictEquals(object? left, object? right)
    {
        if (left == null && right == null) return BoxedConstants.True;
        if (left == null || right == null) return BoxedConstants.False;
        if (IsNaN(left) || IsNaN(right)) return BoxedConstants.False;
        if (left.GetType() != right.GetType()) return BoxedConstants.False;
        return left.Equals(right) ? BoxedConstants.True : BoxedConstants.False;
    }

    public static object StrictNotEquals(object? left, object? right)
    {
        return !(bool)StrictEquals(left, right) ? BoxedConstants.True : BoxedConstants.False;
    }

    public static object LessThan(object? left, object? right, StringComparison comparison)
        => CompareOp(left, right, comparison, static cmp => cmp < 0, "op_LessThan", TokenType.Less);

    public static object LessThanOrEqual(object? left, object? right, StringComparison comparison)
        => CompareOp(left, right, comparison, static cmp => cmp <= 0, "op_LessThanOrEqual", TokenType.LessEqual);

    public static object GreaterThan(object? left, object? right, StringComparison comparison)
        => CompareOp(left, right, comparison, static cmp => cmp > 0, "op_GreaterThan", TokenType.Greater);

    public static object GreaterThanOrEqual(object? left, object? right, StringComparison comparison)
        => CompareOp(left, right, comparison, static cmp => cmp >= 0, "op_GreaterThanOrEqual", TokenType.GreaterEqual);

    private static object CompareOp(
        object? left, object? right, StringComparison comparison,
        Func<int, bool> predicate, string opMethodName, TokenType token)
    {
        if (left == null || right == null) return BoxedConstants.False;
        if (IsNaN(left) || IsNaN(right)) return BoxedConstants.False;
        if (TryCompare(left, right, comparison, out var cmp))
            return predicate(cmp) ? BoxedConstants.True : BoxedConstants.False;
        if (TryInvokeUserDefinedBinaryOperator(left, right, opMethodName, out var r))
            return r ?? BoxedConstants.False;
        throw new AlderException(DiagnosticDescriptors.BadBinaryOps,
            TokenLexemes.GetCanonical(token), TypeNameFormatter.Of(left), TypeNameFormatter.Of(right));
    }

    private static bool IsNaN(object? value) => value switch
    {
        double d => double.IsNaN(d),
        float f => float.IsNaN(f),
        _ => false
    };

    private static bool TryCompare(object? left, object? right, StringComparison comparison, out int result)
    {
        result = 0;
        if (TypeHelpers.IsArithmetic(left) && TypeHelpers.IsArithmetic(right))
        {
            result = NumericDispatch.Compare(left!, right!);
            return true;
        }

        if (left is string ls && right is string rs)
        {
            result = string.Compare(ls, rs, comparison);
            return true;
        }

        if (left is IComparable comparable)
        {
            result = comparable.CompareTo(right);
            return true;
        }

        return false;
    }

    internal static int Compare(object? left, object? right, StringComparison comparison)
    {
        if (TryCompare(left, right, comparison, out var result))
            return result;
        throw new AlderException(DiagnosticDescriptors.BadBinaryOps, TokenLexemes.GetCanonical(TokenType.LessEqualGreater), TypeNameFormatter.Of(left), TypeNameFormatter.Of(right));
    }

    public static object? BitwiseAnd(object? left, object? right)
    {
        if (left is bool lb && right is bool rb)
            return lb & rb ? BoxedConstants.True : BoxedConstants.False;

        // ECMA-334 §12.13.5: Three-value bool? logic for &
        if (left is bool or null && right is bool or null)
        {
            if (left is false || right is false)
                return BoxedConstants.False;
            return null;
        }

        return BitwiseIntegerOp(left, right, NumericDispatch.BitwiseAnd, "op_BitwiseAnd", TokenType.Amp);
    }

    public static object? BitwiseOr(object? left, object? right)
    {
        if (left is bool lb && right is bool rb)
            return lb | rb ? BoxedConstants.True : BoxedConstants.False;

        // ECMA-334 §12.13.5: Three-value bool? logic for |
        if (left is bool or null && right is bool or null)
        {
            if (left is true || right is true)
                return BoxedConstants.True;
            return null;
        }

        return BitwiseIntegerOp(left, right, NumericDispatch.BitwiseOr, "op_BitwiseOr", TokenType.Pipe);
    }

    public static object? BitwiseXor(object? left, object? right)
    {
        if (left is bool lb && right is bool rb)
            return lb ^ rb ? BoxedConstants.True : BoxedConstants.False;

        // ECMA-334 §12.13.5: Three-value bool? logic for ^
        if (left is bool or null && right is bool or null && (left == null || right == null))
            return null;

        return BitwiseIntegerOp(left, right, NumericDispatch.BitwiseXor, "op_ExclusiveOr", TokenType.Caret);
    }

    private static object? BitwiseIntegerOp(object? left, object? right,
        NumericDispatch.BinaryOp dispatch, string opMethodName, TokenType token)
    {
        // ECMA-334 §12.4.8: Lifted integer operators return null when either operand is null
        if (left == null || right == null)
        {
            if (IsIntegerOrChar(left) || IsIntegerOrChar(right))
                return null;
        }

        // ECMA-334 §12.13.3: E op E → E
        if (left != null && right != null && left.GetType().IsEnum && right.GetType().IsEnum)
            return EnumArithmetic.BitwiseOp(left, right, (a, b) => dispatch(a, b));

        if (IsIntegerOrChar(left) && IsIntegerOrChar(right))
            return dispatch(left!, right!);

        if (left != null && right != null &&
            TryInvokeUserDefinedBinaryOperator(left, right, opMethodName, out var userResult))
            return userResult;

        throw new AlderException(DiagnosticDescriptors.BadBinaryOps,
            TokenLexemes.GetCanonical(token), TypeNameFormatter.Of(left), TypeNameFormatter.Of(right));
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
        throw new AlderException(
            DiagnosticDescriptors.BadUnaryOp,
            TokenLexemes.GetCanonical(TokenType.Tilde),
            TypeNameFormatter.Of(value));
    }

    public static object? LeftShift(object? left, object? right)
        => ShiftOp(left, right, NumericDispatch.LeftShift, TokenType.LessLess, "op_LeftShift");

    public static object? RightShift(object? left, object? right)
        => ShiftOp(left, right, NumericDispatch.RightShift, TokenType.GreaterGreater, "op_RightShift");

    public static object? UnsignedRightShift(object? left, object? right)
        => ShiftOp(left, right, NumericDispatch.UnsignedRightShift, TokenType.GreaterGreaterGreater, "op_UnsignedRightShift");

    private static object? ShiftOp(object? left, object? right,
        NumericDispatch.BinaryOp dispatch, TokenType token, string operatorName)
    {
        // ECMA-334 §12.4.8: Lifted operators return null when either operand is null
        if (left == null || right == null)
            return null;

        // ECMA-334 §12.11, §12.4.5: Try user-defined operator overload first
        if (left != null && right != null &&
            TryInvokeUserDefinedBinaryOperator(left, right, operatorName, out var result))
            return result;

        // ECMA-334 §12.11: Predefined shift operators accept integer types and char
        if (!IsIntegerOrChar(left) || !TypeHelpers.IsInteger(right))
            throw new AlderException(DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(token), TypeNameFormatter.Of(left), TypeNameFormatter.Of(right));

        return dispatch(left!, right!);
    }

    private static bool IsIntegerOrChar(object? value) =>
        TypeHelpers.IsInteger(value) || value is char;

    public static bool Contains(object? collection, object? value)
    {
        if (collection == null)
            throw new AlderException(
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

        throw new AlderException(
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

    /// <summary>
    /// ECMA-334 §12.4.5: searches both operand types for a matching binary operator method.
    /// Checks checked variant first when isChecked is true (e.g., op_CheckedAddition before op_Addition).
    /// </summary>
    private static bool TryInvokeUserDefinedBinaryOperator(
        object left, object right, string operatorName, out object? result, bool isChecked = false)
    {
        result = null;
        var leftType = left.GetType();
        var rightType = right.GetType();

        if (isChecked)
        {
            var checkedName = operatorName.Replace("op_", "op_Checked");
            var checkedMethod = ResolveUserDefinedBinaryOperator(leftType, rightType, checkedName);
            if (checkedMethod != null)
                return InvokeOperator(checkedMethod, left, right, out result);
        }

        var method = ResolveUserDefinedBinaryOperator(leftType, rightType, operatorName);
        if (method != null)
            return InvokeOperator(method, left, right, out result);

        return false;
    }

    private static MethodInfo? ResolveUserDefinedBinaryOperator(Type leftType, Type rightType, string operatorName)
    {
        return UserDefinedBinaryOperatorCache.GetOrAdd(
            (leftType, rightType, operatorName),
            static key =>
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
                var searchTypes = key.Left == key.Right ? new[] { key.Left } : new[] { key.Left, key.Right };

                foreach (var type in searchTypes)
                {
                    foreach (var method in ReflectionRuntime.GetMethods(type, flags))
                    {
                        if (!string.Equals(method.Name, key.Op, StringComparison.Ordinal))
                            continue;

                        var parameters = method.GetParameters();
                        if (parameters.Length != 2)
                            continue;

                        var p0 = parameters[0].ParameterType;
                        var p1 = parameters[1].ParameterType;
                        if ((p0.IsAssignableFrom(key.Left) || TypeHelpers.CanImplicitlyConvert(key.Left, p0)) &&
                            (p1.IsAssignableFrom(key.Right) || TypeHelpers.CanImplicitlyConvert(key.Right, p1)))
                            return method;
                    }
                }

                return null;
            });
    }

    private static bool InvokeOperator(MethodInfo method, object left, object right, out object? result)
    {
        try
        {
            var parameters = method.GetParameters();
            var p0 = parameters[0].ParameterType;
            var p1 = parameters[1].ParameterType;
            result = method.Invoke(null, [
                p0.IsInstanceOfType(left) ? left : Convert.ChangeType(left, p0),
                p1.IsInstanceOfType(right) ? right : Convert.ChangeType(right, p1),
            ]);
            return true;
        }
        catch (TargetInvocationException tie) when (tie.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
            throw;
        }
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
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.Star),
                TypeNameFormatter.Of(left), TypeNameFormatter.Of(right));
        }

        if (countObj == null)
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.Star),
                TypeNameFormatter.Of(left), TypeNameFormatter.Of(right));

        if (!TypeHelpers.IsInteger(countObj))
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.Star),
                TypeNameFormatter.Of(left), TypeNameFormatter.Of(right));

        int count = Convert.ToInt32(countObj);
        if (count < 0)
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.Star),
                TypeNameFormatter.Of(left), TypeNameFormatter.Of(right));
        if (count == 0)
            return "";

        return string.Concat(Enumerable.Repeat(str, count));
    }

    public static bool Like(object? left, object? right, StringComparison comparison)
    {
        if (left is not string str || right is not string pattern)
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.Like),
                TypeNameFormatter.Of(left),
                TypeNameFormatter.Of(right));

        if (pattern.Length == 0)
            return str.Length == 0;

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
                (pattern[patternIndex] == '_' || (ignoreCase ? char.ToUpperInvariant(pattern[patternIndex]) == char.ToUpperInvariant(value[valueIndex]) : pattern[patternIndex] == value[valueIndex])))
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

    public static object RegexMatch(object? left, object? right)
    {
        return Extensions.RegexMatchOperator.IsMatch(left, right) ? BoxedConstants.True : BoxedConstants.False;
    }

    public static object RegexNotMatch(object? left, object? right)
    {
        return Extensions.RegexMatchOperator.IsNotMatch(left, right) ? BoxedConstants.True : BoxedConstants.False;
    }

    public static object Spaceship(object? left, object? right)
    {
        return Extensions.SpaceshipOperator.Compare(left, right) switch
        {
            -1 => BoxedConstants.Int32MinusOne,
            0 => BoxedConstants.Int32Zero,
            _ => BoxedConstants.Int32One
        };
    }

}
