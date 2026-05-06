using System.Runtime.ExceptionServices;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime.Collections;

namespace Alder.Runtime;

/// <summary>
/// Performs numeric operator dispatch without relying on <c>dynamic</c> or late-bound reflection.
/// ECMA-334 §12.4.7.2 and §12.4.7.3 define the promotion rules that decide which precomputed operator table entry applies.
/// </summary>
internal static class NumericDispatch
{
    private static readonly string SpaceshipOperatorLexeme = TokenLexemes.GetCanonical(TokenType.LessEqualGreater);

    public delegate object? BinaryOp(object left, object right);
    public delegate object? UnaryOp(object value);
    public delegate int CompareOp(object left, object right);

    private static readonly FixedDictionary<(Type, Type), BinaryOp> AddOps = BuildBinaryOps(
        (int l, int r) => l + r,
        (long l, long r) => l + r,
        (float l, float r) => l + r,
        (double l, double r) => l + r,
        (decimal l, decimal r) => l + r,
        (uint l, uint r) => l + r,
        (ulong l, ulong r) => l + r
    );

    private static readonly FixedDictionary<(Type, Type), BinaryOp> SubtractOps = BuildBinaryOps(
        (int l, int r) => l - r,
        (long l, long r) => l - r,
        (float l, float r) => l - r,
        (double l, double r) => l - r,
        (decimal l, decimal r) => l - r,
        (uint l, uint r) => l - r,
        (ulong l, ulong r) => l - r
    );

    private static readonly FixedDictionary<(Type, Type), BinaryOp> MultiplyOps = BuildBinaryOps(
        (int l, int r) => l * r,
        (long l, long r) => l * r,
        (float l, float r) => l * r,
        (double l, double r) => l * r,
        (decimal l, decimal r) => l * r,
        (uint l, uint r) => l * r,
        (ulong l, ulong r) => l * r
    );

    private static readonly FixedDictionary<(Type, Type), BinaryOp> DivideOps = BuildBinaryOps(
        (int l, int r) => l / r,
        (long l, long r) => l / r,
        (float l, float r) => l / r,
        (double l, double r) => l / r,
        (decimal l, decimal r) => l / r,
        (uint l, uint r) => l / r,
        (ulong l, ulong r) => l / r
    );

    private static readonly FixedDictionary<(Type, Type), BinaryOp> ModuloOps = BuildBinaryOps(
        (int l, int r) => l % r,
        (long l, long r) => l % r,
        (float l, float r) => l % r,
        (double l, double r) => l % r,
        (decimal l, decimal r) => l % r,
        (uint l, uint r) => l % r,
        (ulong l, ulong r) => l % r
    );

    private static readonly FixedDictionary<(Type, Type), BinaryOp> BitwiseAndOps = BuildIntegerBinaryOps(
        (int l, int r) => l & r,
        (long l, long r) => l & r,
        (uint l, uint r) => l & r,
        (ulong l, ulong r) => l & r
    );

    private static readonly FixedDictionary<(Type, Type), BinaryOp> BitwiseOrOps = BuildIntegerBinaryOps(
        (int l, int r) => l | r,
        (long l, long r) => l | r,
        (uint l, uint r) => l | r,
        (ulong l, ulong r) => l | r
    );

    private static readonly FixedDictionary<(Type, Type), BinaryOp> BitwiseXorOps = BuildIntegerBinaryOps(
        (int l, int r) => l ^ r,
        (long l, long r) => l ^ r,
        (uint l, uint r) => l ^ r,
        (ulong l, ulong r) => l ^ r
    );

    private static readonly FixedDictionary<(Type, Type), BinaryOp> CheckedAddOps = BuildBinaryOps(
        (int l, int r) => checked(l + r),
        (long l, long r) => checked(l + r),
        (float l, float r) => l + r,
        (double l, double r) => l + r,
        (decimal l, decimal r) => checked(l + r),
        (uint l, uint r) => checked(l + r),
        (ulong l, ulong r) => checked(l + r)
    );

    private static readonly FixedDictionary<(Type, Type), BinaryOp> CheckedSubtractOps = BuildBinaryOps(
        (int l, int r) => checked(l - r),
        (long l, long r) => checked(l - r),
        (float l, float r) => l - r,
        (double l, double r) => l - r,
        (decimal l, decimal r) => checked(l - r),
        (uint l, uint r) => checked(l - r),
        (ulong l, ulong r) => checked(l - r)
    );

    private static readonly FixedDictionary<(Type, Type), BinaryOp> CheckedMultiplyOps = BuildBinaryOps(
        (int l, int r) => checked(l * r),
        (long l, long r) => checked(l * r),
        (float l, float r) => l * r,
        (double l, double r) => l * r,
        (decimal l, decimal r) => checked(l * r),
        (uint l, uint r) => checked(l * r),
        (ulong l, ulong r) => checked(l * r)
    );

    private static readonly FixedDictionary<(Type, Type), CompareOp> CompareOps = BuildCompareOps();

    private static readonly FixedDictionary<Type, UnaryOp> NegateOps = FixedDictionary<Type, UnaryOp>.Create(new Dictionary<Type, UnaryOp>
    {
        [typeof(int)] = v => -(int)v,
        [typeof(long)] = v => -(long)v,
        [typeof(float)] = v => -(float)v,
        [typeof(double)] = v => -(double)v,
        [typeof(decimal)] = v => -(decimal)v,
        // ECMA-334 §12.4.7.2: unary numeric promotion lifts the small integral types to int.
        [typeof(short)] = v => -(int)(short)v,
        [typeof(sbyte)] = v => -(int)(sbyte)v,
        [typeof(byte)] = v => -(int)(byte)v,
        [typeof(ushort)] = v => -(int)(ushort)v,
        // ECMA-334 §12.9.3: applying unary minus to uint produces a long result.
        [typeof(uint)] = v => -(long)(uint)v,
    });

    private static readonly FixedDictionary<Type, UnaryOp> CheckedNegateOps = FixedDictionary<Type, UnaryOp>.Create(new Dictionary<Type, UnaryOp>
    {
        [typeof(int)] = v => checked(-(int)v),
        [typeof(long)] = v => checked(-(long)v),
        [typeof(float)] = v => -(float)v,
        [typeof(double)] = v => -(double)v,
        [typeof(decimal)] = v => checked(-(decimal)v),
        [typeof(short)] = v => checked(-(int)(short)v),
        [typeof(sbyte)] = v => checked(-(int)(sbyte)v),
        [typeof(byte)] = v => -(int)(byte)v,
        [typeof(ushort)] = v => -(int)(ushort)v,
        // ECMA-334 §12.9.3: applying unary minus to uint produces a long result.
        [typeof(uint)] = v => checked(-(long)(uint)v),
    });

    private static readonly FixedDictionary<Type, UnaryOp> BitwiseNotOps = FixedDictionary<Type, UnaryOp>.Create(new Dictionary<Type, UnaryOp>
    {
        [typeof(int)] = v => ~(int)v,
        [typeof(long)] = v => ~(long)v,
        [typeof(uint)] = v => ~(uint)v,
        [typeof(ulong)] = v => ~(ulong)v,
        // ECMA-334 §12.4.7.2: unary numeric promotion lifts the small integral types to int.
        [typeof(short)] = v => ~(int)(short)v,
        [typeof(ushort)] = v => ~(int)(ushort)v,
        [typeof(byte)] = v => ~(int)(byte)v,
        [typeof(sbyte)] = v => ~(int)(sbyte)v,
    });

    private delegate object ShiftOp(object value, int shiftAmount);

    // ECMA-334 §12.11: predefined shift operators are defined for int, uint, long, ulong.
    // Small integral types promote to int through overload resolution.
    private static readonly FixedDictionary<Type, ShiftOp> LeftShiftOps = BuildShiftOps(
        (int v, int s) => v << s, (long v, int s) => v << s,
        (uint v, int s) => v << s, (ulong v, int s) => v << s);

    private static readonly FixedDictionary<Type, ShiftOp> RightShiftOps = BuildShiftOps(
        (int v, int s) => v >> s, (long v, int s) => v >> s,
        (uint v, int s) => v >> s, (ulong v, int s) => v >> s);

    // C# 11 §12.11: unsigned right shift treats the left operand as unsigned.
    private static readonly FixedDictionary<Type, ShiftOp> UnsignedRightShiftOps = BuildShiftOps(
        (int v, int s) => (int)((uint)v >> (s & 0x1F)),
        (long v, int s) => (long)((ulong)v >> (s & 0x3F)),
        (uint v, int s) => v >> (s & 0x1F),
        (ulong v, int s) => v >> (s & 0x3F));

    public static object? Add(object left, object right, bool isChecked = false)
        => ExecuteBinaryOp(left, right, isChecked ? CheckedAddOps : AddOps, "+");

    public static object? Subtract(object left, object right, bool isChecked = false)
        => ExecuteBinaryOp(left, right, isChecked ? CheckedSubtractOps : SubtractOps, "-");

    public static object? Multiply(object left, object right, bool isChecked = false)
        => ExecuteBinaryOp(left, right, isChecked ? CheckedMultiplyOps : MultiplyOps, "*");

    public static object? Divide(object left, object right)
        => ExecuteBinaryOp(left, right, DivideOps, "/");

    public static object? Modulo(object left, object right)
        => ExecuteBinaryOp(left, right, ModuloOps, "%");

    public static object? BitwiseAnd(object left, object right)
        => ExecuteBinaryOp(left, right, BitwiseAndOps, "&");

    public static object? BitwiseOr(object left, object right)
        => ExecuteBinaryOp(left, right, BitwiseOrOps, "|");

    public static object? BitwiseXor(object left, object right)
        => ExecuteBinaryOp(left, right, BitwiseXorOps, "^");

    public static object? Negate(object value, bool isChecked = false)
    {
        var type = value.GetType();

        // ECMA-334 §12.4.7.2: char participates through unary numeric promotion to int.
        if (type == typeof(char))
        {
            value = (int)(char)value;
            type = typeof(int);
        }

        var ops = isChecked ? CheckedNegateOps : NegateOps;
        if (ops.TryGetValue(type, out var op))
            return op(value);

        if (TryFindUnaryNegationOperator(type, isChecked, out var operatorMethod))
        {
            try
            {
                return operatorMethod.Invoke(null, [value]);
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
                throw;
            }
        }

        throw new AlderException(DiagnosticDescriptors.BadUnaryOp, TokenLexemes.GetCanonical(TokenType.Minus), type.Name);
    }

    private static bool TryFindUnaryNegationOperator(Type operandType, bool isChecked, out MethodInfo method)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        var methods = RuntimeTypeIntrospection.GetMethods(operandType, flags);

        if (isChecked)
        {
            var checkedMethod = methods.FirstOrDefault(candidate =>
            {
                if (!string.Equals(candidate.Name, "op_CheckedUnaryNegation", StringComparison.Ordinal))
                    return false;

                var parameters = candidate.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == operandType;
            });
            if (checkedMethod != null)
            {
                method = checkedMethod;
                return true;
            }
        }

        var regularMethod = methods.FirstOrDefault(candidate =>
        {
            if (!string.Equals(candidate.Name, "op_UnaryNegation", StringComparison.Ordinal))
                return false;

            var parameters = candidate.GetParameters();
            return parameters.Length == 1 && parameters[0].ParameterType == operandType;
        });
        if (regularMethod != null)
        {
            method = regularMethod;
            return true;
        }

        method = null!;
        return false;
    }

    public static object? UnaryPlus(object value)
    {
        var type = value.GetType();

        // Per ECMA-334 §12.4.7.2, char is promoted to int
        if (type == typeof(char))
            return (int)(char)value;

        return Type.GetTypeCode(type) switch
        {
            TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 => Convert.ToInt32(value),
            _ => value
        };
    }

    public static object? BitwiseNot(object value)
    {
        var type = value.GetType();

        // ECMA-334 §12.4.7.2: Unary numeric promotion for ~
        if (type == typeof(char))
        {
            value = (int)(char)value;
            type = typeof(int);
        }

        if (BitwiseNotOps.TryGetValue(type, out var op))
            return op(value);

        throw new AlderException(DiagnosticDescriptors.BadUnaryOp, TokenLexemes.GetCanonical(TokenType.Tilde), type.Name);
    }

    public static int Compare(object left, object right)
    {
        var leftType = left.GetType();
        var rightType = right.GetType();

        if (leftType == rightType && CompareOps.TryGetValue((leftType, leftType), out var fastOp))
            return fastOp(left, right);

        var (promotedLeft, promotedRight, resultType) = PromoteOperands(left, right);

        if (CompareOps.TryGetValue((resultType, resultType), out var op))
            return op(promotedLeft, promotedRight);

        throw new AlderException(
            DiagnosticDescriptors.BadBinaryOps,
            SpaceshipOperatorLexeme,
            leftType.Name,
            rightType.Name);
    }

    public static object? LeftShift(object left, object right)
        => ExecuteShiftOp(left, right, LeftShiftOps, TokenType.LessLess);

    public static object? RightShift(object left, object right)
        => ExecuteShiftOp(left, right, RightShiftOps, TokenType.GreaterGreater);

    public static object? UnsignedRightShift(object left, object right)
        => ExecuteShiftOp(left, right, UnsignedRightShiftOps, TokenType.GreaterGreaterGreater);

    // ECMA-334 §10.2.11: Constant expression promotion

    /// <summary>
    /// ECMA-334 §10.2.11: Implicit constant expression conversions.
    /// A constant expression of type int can be implicitly converted to uint if the value
    /// is in range [0, uint.MaxValue]. A constant expression of type int or long can be
    /// implicitly converted to ulong if the value is non-negative.
    /// Returns the promoted operands and result type, or null if constant promotion doesn't apply.
    /// </summary>
    public static (object Left, object Right, Type ResultType)? TryConstantPromotion(
        object left, bool leftIsConstant, object right, bool rightIsConstant)
    {
        // Try right constant promoting to left's type, then left constant promoting to right's type.
        if (rightIsConstant)
        {
            var result = TryPromoteConstant(right, left.GetType());
            if (result != null) return (left, result, left.GetType());
        }
        if (leftIsConstant)
        {
            var result = TryPromoteConstant(left, right.GetType());
            if (result != null) return (result, right, right.GetType());
        }
        return null;
    }

    /// <summary>
    /// §10.2.11: non-negative int → uint, non-negative int/long → ulong.
    /// </summary>
    private static object? TryPromoteConstant(object constant, Type targetType)
    {
        if (targetType == typeof(uint) && constant is int intToUint && intToUint >= 0)
            return (uint)intToUint;
        if (targetType == typeof(ulong) && constant is int intToUlong && intToUlong >= 0)
            return (ulong)intToUlong;
        if (targetType == typeof(ulong) && constant is long longToUlong && longToUlong >= 0)
            return (ulong)longToUlong;
        return null;
    }

    // ECMA-334 §12.4.7.3: Type promotion

    /// <summary>
    /// Non-throwing variant: returns the ECMA-334 binary numeric promotion result type,
    /// or null when the combination is invalid (e.g. ulong + signed integer).
    /// </summary>
    public static Type? TryGetResultType(Type leftType, Type rightType)
    {
        if (leftType == typeof(decimal) || rightType == typeof(decimal))
            return typeof(decimal);
        if (leftType == typeof(double) || rightType == typeof(double))
            return typeof(double);
        if (leftType == typeof(float) || rightType == typeof(float))
            return typeof(float);
        if (leftType == typeof(ulong) || rightType == typeof(ulong))
        {
            if (IsSignedInteger(leftType) || IsSignedInteger(rightType))
                return null;
            return typeof(ulong);
        }
        if (leftType == typeof(long) || rightType == typeof(long))
            return typeof(long);
        if ((leftType == typeof(uint) && IsSignedInteger(rightType)) ||
            (rightType == typeof(uint) && IsSignedInteger(leftType)))
            return typeof(long);
        if (leftType == typeof(uint) || rightType == typeof(uint))
            return typeof(uint);
        return typeof(int);
    }

    /// <summary>
    /// Gets the result type for binary numeric operations according to ECMA-334 rules.
    /// Throws for invalid combinations (e.g. ulong + signed integer).
    /// </summary>
    public static Type GetResultType(Type leftType, Type rightType)
    {
        return TryGetResultType(leftType, rightType)
            ?? throw new AlderException(DiagnosticDescriptors.BadBinaryOps, TokenLexemes.GetCanonical(TokenType.Plus), leftType.Name, rightType.Name);
    }

    /// <summary>
    /// Promotes two operands according to ECMA-334 §12.4.7.3 binary numeric promotion rules.
    /// Returns the promoted values and the result type.
    ///
    /// IMPORTANT: char is NOT eagerly converted to int. Per §10.2.3, char has implicit
    /// conversions to ushort, int, uint, long, ulong, float, double, decimal.
    /// Rule 6 checks for "sbyte, short, or int" -- char is none of these.
    /// So uint + char -> uint (Rule 7), not long (Rule 6).
    /// </summary>
    public static (object Left, object Right, Type ResultType) PromoteOperands(object left, object right)
    {
        var leftType = left.GetType();
        var rightType = right.GetType();

        // Rule 1: decimal (error if mixed with float/double)
        if (leftType == typeof(decimal) || rightType == typeof(decimal))
        {
            if (leftType == typeof(float) || leftType == typeof(double) ||
                rightType == typeof(float) || rightType == typeof(double))
            {
                throw new AlderException(DiagnosticDescriptors.BadBinaryOps, TokenLexemes.GetCanonical(TokenType.Plus), leftType.Name, rightType.Name);
            }
            return (ConvertToDecimal(left), ConvertToDecimal(right), typeof(decimal));
        }

        // Rule 2: double
        if (leftType == typeof(double) || rightType == typeof(double))
            return (ConvertToDouble(left), ConvertToDouble(right), typeof(double));

        // Rule 3: float
        if (leftType == typeof(float) || rightType == typeof(float))
            return (ConvertToSingle(left), ConvertToSingle(right), typeof(float));

        // Rule 4: ulong (error if other operand is a signed integer type)
        // Note: char is NOT signed, so char -> ulong is valid per §10.2.3
        if (leftType == typeof(ulong) || rightType == typeof(ulong))
        {
            if (IsSignedInteger(leftType) || IsSignedInteger(rightType))
            {
                throw new AlderException(DiagnosticDescriptors.BadBinaryOps, TokenLexemes.GetCanonical(TokenType.Plus), leftType.Name, rightType.Name);
            }
            return (ConvertToUInt64(left), ConvertToUInt64(right), typeof(ulong));
        }

        // Rule 5: long
        if (leftType == typeof(long) || rightType == typeof(long))
            return (ConvertToInt64(left), ConvertToInt64(right), typeof(long));

        // Rule 6: uint + signed -> long
        // ECMA-334 §12.4.7.3: "if either operand is of type uint and the other operand is
        // of type sbyte, short, or int" -- char is NOT listed here
        if ((leftType == typeof(uint) && IsSignedInteger(rightType)) ||
            (rightType == typeof(uint) && IsSignedInteger(leftType)))
            return (Convert.ToInt64(left), Convert.ToInt64(right), typeof(long));

        // Rule 7: uint (char -> uint is valid per §10.2.3)
        if (leftType == typeof(uint) || rightType == typeof(uint))
            return (ConvertToUInt32(left), ConvertToUInt32(right), typeof(uint));

        // Rule 8: default to int (includes byte, sbyte, short, ushort, char)
        return (ConvertToInt32(left), ConvertToInt32(right), typeof(int));
    }

    /// <summary>
    /// Checks if a type is a signed integer type as listed in ECMA-334 §12.4.7.3 Rule 6.
    /// Note: char is NOT a signed integer type.
    /// </summary>
    private static bool IsSignedInteger(Type type) =>
        type == typeof(sbyte) || type == typeof(short) || type == typeof(int) || type == typeof(long);

    // Conversion helpers that handle char -> numeric conversions per §10.2.3
    private static decimal ConvertToDecimal(object value) =>
        value is char c ? (decimal)c : Convert.ToDecimal(value);

    private static double ConvertToDouble(object value) =>
        value is char c ? (double)c : Convert.ToDouble(value);

    private static float ConvertToSingle(object value) =>
        value is char c ? (float)c : Convert.ToSingle(value);

    private static ulong ConvertToUInt64(object value) =>
        value is char c ? (ulong)c : Convert.ToUInt64(value);

    private static long ConvertToInt64(object value) =>
        value is char c ? (long)c : Convert.ToInt64(value);

    private static uint ConvertToUInt32(object value) =>
        value is char c ? (uint)c : Convert.ToUInt32(value);

    private static int ConvertToInt32(object value) =>
        value is char c ? (int)c : Convert.ToInt32(value);

    /// <summary>
    /// Promotes a value to the target type according to numeric promotion rules.
    /// Used for ternary operator type unification (ECMA-334 §12.18).
    /// </summary>
    public static object? PromoteToType(object? value, Type targetType)
    {
        if (value == null) return null;

        var sourceType = value.GetType();
        if (sourceType == targetType) return value;

        return Type.GetTypeCode(targetType) switch
        {
            TypeCode.Int64 => Convert.ToInt64(value),
            TypeCode.Double => Convert.ToDouble(value),
            TypeCode.Single => Convert.ToSingle(value),
            TypeCode.Decimal => Convert.ToDecimal(value),
            TypeCode.UInt64 => Convert.ToUInt64(value),
            TypeCode.UInt32 => Convert.ToUInt32(value),
            TypeCode.Int32 => Convert.ToInt32(value),
            _ => value
        };
    }

    public static object Add(object left, object right, Type promotedType, bool isChecked)
        => ExecutePromotedBinaryOp(left, right, promotedType, isChecked ? CheckedAddOps : AddOps, "+");

    public static object Subtract(object left, object right, Type promotedType, bool isChecked)
        => ExecutePromotedBinaryOp(left, right, promotedType, isChecked ? CheckedSubtractOps : SubtractOps, "-");

    public static object Multiply(object left, object right, Type promotedType, bool isChecked)
        => ExecutePromotedBinaryOp(left, right, promotedType, isChecked ? CheckedMultiplyOps : MultiplyOps, "*");

    public static object Divide(object left, object right, Type promotedType)
        => ExecutePromotedBinaryOp(left, right, promotedType, DivideOps, "/");

    public static object Modulo(object left, object right, Type promotedType)
        => ExecutePromotedBinaryOp(left, right, promotedType, ModuloOps, "%");

    public static int Compare(object left, object right, Type promotedType)
    {
        var key = (promotedType, promotedType);
        if (CompareOps.TryGetValue(key, out var op))
            return op(PromoteToType(left, promotedType)!, PromoteToType(right, promotedType)!);

        return Compare(left, right);
    }

    public static object BitwiseAnd(object left, object right, Type promotedType)
        => ExecutePromotedBinaryOp(left, right, promotedType, BitwiseAndOps, "&");

    public static object BitwiseOr(object left, object right, Type promotedType)
        => ExecutePromotedBinaryOp(left, right, promotedType, BitwiseOrOps, "|");

    public static object BitwiseXor(object left, object right, Type promotedType)
        => ExecutePromotedBinaryOp(left, right, promotedType, BitwiseXorOps, "^");

    public static object? Negate(object value, Type promotedType, bool isChecked)
    {
        var promoted = PromoteToType(value, promotedType)!;
        var ops = isChecked ? CheckedNegateOps : NegateOps;
        if (ops.TryGetValue(promotedType, out var op))
            return op(promoted);

        return Negate(value, isChecked);
    }

    public static object? BitwiseNot(object value, Type promotedType)
    {
        var promoted = PromoteToType(value, promotedType)!;
        if (BitwiseNotOps.TryGetValue(promotedType, out var op))
            return op(promoted);

        return BitwiseNot(value);
    }

    public static object? UnaryPlus(object value, Type promotedType)
    {
        return PromoteToType(value, promotedType);
    }

    private static object ExecuteShiftOp(
        object left, object right,
        FixedDictionary<Type, ShiftOp> ops,
        TokenType opToken)
    {
        var shiftAmount = Convert.ToInt32(right);
        if (left is char c) left = (int)c;
        var type = left.GetType();
        if (ops.TryGetValue(type, out var op))
            return op(left, shiftAmount);
        throw new AlderException(DiagnosticDescriptors.BadBinaryOps, TokenLexemes.GetCanonical(opToken), type.Name, right.GetType().Name);
    }

    private static object ExecutePromotedBinaryOp(
        object left, object right,
        Type promotedType,
        FixedDictionary<(Type, Type), BinaryOp> ops,
        string opName)
    {
        var key = (promotedType, promotedType);
        if (ops.TryGetValue(key, out var op))
            return op(PromoteToType(left, promotedType)!, PromoteToType(right, promotedType)!)!;

        throw new AlderException(DiagnosticDescriptors.BadBinaryOps, opName, left.GetType().Name, right.GetType().Name);
    }

    private static object ExecuteBinaryOp(
        object left, object right,
        FixedDictionary<(Type, Type), BinaryOp> ops,
        string opName)
    {
        var leftType = left.GetType();
        var rightType = right.GetType();

        // Fast path: same-type operands skip PromoteOperands (avoids 2 unbox+rebox cycles).
        if (leftType == rightType && ops.TryGetValue((leftType, leftType), out var fastOp))
            return fastOp(left, right)!;

        var (promotedLeft, promotedRight, resultType) = PromoteOperands(left, right);

        if (ops.TryGetValue((resultType, resultType), out var op))
            return op(promotedLeft, promotedRight)!;

        throw new AlderException(DiagnosticDescriptors.BadBinaryOps, opName, leftType.Name, rightType.Name);
    }

    private static FixedDictionary<(Type, Type), BinaryOp> BuildBinaryOps(
        Func<int, int, int> intOp,
        Func<long, long, long> longOp,
        Func<float, float, float> floatOp,
        Func<double, double, double> doubleOp,
        Func<decimal, decimal, decimal> decimalOp,
        Func<uint, uint, uint> uintOp,
        Func<ulong, ulong, ulong> ulongOp)
    {
        return FixedDictionary<(Type, Type), BinaryOp>.Create(new Dictionary<(Type, Type), BinaryOp>
        {
            [(typeof(int), typeof(int))] = (l, r) => intOp((int)l, (int)r),
            [(typeof(long), typeof(long))] = (l, r) => longOp((long)l, (long)r),
            [(typeof(float), typeof(float))] = (l, r) => floatOp((float)l, (float)r),
            [(typeof(double), typeof(double))] = (l, r) => doubleOp((double)l, (double)r),
            [(typeof(decimal), typeof(decimal))] = (l, r) => decimalOp((decimal)l, (decimal)r),
            [(typeof(uint), typeof(uint))] = (l, r) => uintOp((uint)l, (uint)r),
            [(typeof(ulong), typeof(ulong))] = (l, r) => ulongOp((ulong)l, (ulong)r),
        });
    }

    private static FixedDictionary<(Type, Type), BinaryOp> BuildIntegerBinaryOps(
        Func<int, int, int> intOp,
        Func<long, long, long> longOp,
        Func<uint, uint, uint> uintOp,
        Func<ulong, ulong, ulong> ulongOp)
    {
        return FixedDictionary<(Type, Type), BinaryOp>.Create(new Dictionary<(Type, Type), BinaryOp>
        {
            [(typeof(int), typeof(int))] = (l, r) => intOp((int)l, (int)r),
            [(typeof(long), typeof(long))] = (l, r) => longOp((long)l, (long)r),
            [(typeof(uint), typeof(uint))] = (l, r) => uintOp((uint)l, (uint)r),
            [(typeof(ulong), typeof(ulong))] = (l, r) => ulongOp((ulong)l, (ulong)r),
        });
    }

    private static FixedDictionary<(Type, Type), CompareOp> BuildCompareOps()
    {
        return FixedDictionary<(Type, Type), CompareOp>.Create(new Dictionary<(Type, Type), CompareOp>
        {
            [(typeof(int), typeof(int))] = (l, r) => ((int)l).CompareTo((int)r),
            [(typeof(long), typeof(long))] = (l, r) => ((long)l).CompareTo((long)r),
            [(typeof(float), typeof(float))] = (l, r) => ((float)l).CompareTo((float)r),
            [(typeof(double), typeof(double))] = (l, r) => ((double)l).CompareTo((double)r),
            [(typeof(decimal), typeof(decimal))] = (l, r) => ((decimal)l).CompareTo((decimal)r),
            [(typeof(uint), typeof(uint))] = (l, r) => ((uint)l).CompareTo((uint)r),
            [(typeof(ulong), typeof(ulong))] = (l, r) => ((ulong)l).CompareTo((ulong)r),
        });
    }

    // §12.11: predefined overloads are (int,int), (uint,int), (long,int), (ulong,int).
    // Small types (byte, sbyte, short, ushort) promote to int via overload resolution.
    private static FixedDictionary<Type, ShiftOp> BuildShiftOps(
        Func<int, int, object> intOp,
        Func<long, int, object> longOp,
        Func<uint, int, object> uintOp,
        Func<ulong, int, object> ulongOp)
    {
        return FixedDictionary<Type, ShiftOp>.Create(new Dictionary<Type, ShiftOp>
        {
            [typeof(int)] = (v, s) => intOp((int)v, s),
            [typeof(long)] = (v, s) => longOp((long)v, s),
            [typeof(uint)] = (v, s) => uintOp((uint)v, s),
            [typeof(ulong)] = (v, s) => ulongOp((ulong)v, s),
            [typeof(short)] = (v, s) => intOp((short)v, s),
            [typeof(ushort)] = (v, s) => intOp((ushort)v, s),
            [typeof(byte)] = (v, s) => intOp((byte)v, s),
            [typeof(sbyte)] = (v, s) => intOp((sbyte)v, s),
        });
    }
}
