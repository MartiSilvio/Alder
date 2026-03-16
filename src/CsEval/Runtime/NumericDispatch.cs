using CsEval.Runtime.Collections;
using CsEval.Diagnostics;
using CsEval.Parsing;
using System.Runtime.ExceptionServices;

namespace CsEval.Runtime;

/// <summary>
/// Fast numeric operator dispatch without dynamic.
/// Implements ECMA-334 §12.4.7.3 binary numeric promotion rules.
/// </summary>
internal static class NumericDispatch
{
    private static readonly string SpaceshipOperatorLexeme = TokenLexemes.GetCanonical(TokenType.LessEqualGreater);

    public delegate object BinaryOp(object left, object right);
    public delegate object UnaryOp(object value);
    public delegate int CompareOp(object left, object right);

    #region Binary Operator Delegates

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

    #endregion

    #region Checked Binary Operator Delegates

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

    #endregion

    #region Comparison Delegates

    private static readonly FixedDictionary<(Type, Type), CompareOp> CompareOps = BuildCompareOps();

    #endregion

    #region Unary Operator Delegates

    private static readonly FixedDictionary<Type, UnaryOp> NegateOps = FixedDictionary<Type, UnaryOp>.Create(new Dictionary<Type, UnaryOp>
    {
        [typeof(int)] = v => -(int)v,
        [typeof(long)] = v => -(long)v,
        [typeof(float)] = v => -(float)v,
        [typeof(double)] = v => -(double)v,
        [typeof(decimal)] = v => -(decimal)v,
        // ECMA-334 §12.4.7.2: small types are promoted to int for unary negation
        [typeof(short)] = v => -(int)(short)v,
        [typeof(sbyte)] = v => -(int)(sbyte)v,
        [typeof(byte)] = v => -(int)(byte)v,
        [typeof(ushort)] = v => -(int)(ushort)v,
        // ECMA-334 §12.9.3: uint is converted to long, result type is long
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
        // ECMA-334 §12.9.3: uint is converted to long, result type is long
        [typeof(uint)] = v => checked(-(long)(uint)v),
    });

    private static readonly FixedDictionary<Type, UnaryOp> BitwiseNotOps = FixedDictionary<Type, UnaryOp>.Create(new Dictionary<Type, UnaryOp>
    {
        [typeof(int)] = v => ~(int)v,
        [typeof(long)] = v => ~(long)v,
        [typeof(uint)] = v => ~(uint)v,
        [typeof(ulong)] = v => ~(ulong)v,
        // ECMA-334 §12.4.7.2: small types are promoted to int for bitwise NOT
        [typeof(short)] = v => ~(int)(short)v,
        [typeof(ushort)] = v => ~(int)(ushort)v,
        [typeof(byte)] = v => ~(int)(byte)v,
        [typeof(sbyte)] = v => ~(int)(sbyte)v,
    });

    #endregion

    #region Public API

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
        => ExecuteIntegerBinaryOp(left, right, BitwiseAndOps, "&");

    public static object? BitwiseOr(object left, object right)
        => ExecuteIntegerBinaryOp(left, right, BitwiseOrOps, "|");

    public static object? BitwiseXor(object left, object right)
        => ExecuteIntegerBinaryOp(left, right, BitwiseXorOps, "^");

    public static object? Negate(object value, bool isChecked = false)
    {
        var type = value.GetType();

        // Per ECMA-334 §12.4.7.2, char is promoted to int
        if (type == typeof(char))
        {
            value = (int)(char)value;
            type = typeof(int);
        }

        // Handle int.MinValue edge case: -2147483648 should be int, not long
        // The lexer parses 2147483648 as long, but negated it becomes int.MinValue
        if (type == typeof(long))
        {
            var longVal = (long)value;
            if (longVal == (long)int.MaxValue + 1)
                return int.MinValue;
        }

        // Handle ulong -> long edge case for long.MinValue
        if (type == typeof(ulong))
        {
            var ulongVal = (ulong)value;
            if (ulongVal == (ulong)long.MaxValue + 1)
                return long.MinValue;
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

        throw new CsEvalException(DiagnosticDescriptors.BadUnaryOp, "-", type.Name);
    }

    private static bool TryFindUnaryNegationOperator(Type operandType, bool isChecked, out MethodInfo method)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        var methods = ReflectionRuntime.GetMethods(operandType, flags);

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

        // For all numeric types, unary + returns the value (with promotion to int for small types)
        return type.Name switch
        {
            "SByte" or "Byte" or "Int16" or "UInt16" => Convert.ToInt32(value),
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

        throw new CsEvalException(DiagnosticDescriptors.BadUnaryOp, "~", type.Name);
    }

    public static int Compare(object left, object right)
    {
        var (promotedLeft, promotedRight, resultType) = PromoteOperands(left, right);
        var key = (resultType, resultType);

        if (CompareOps.TryGetValue(key, out var op))
            return op(promotedLeft, promotedRight);

        throw new CsEvalException(
            DiagnosticDescriptors.BadBinaryOps,
            SpaceshipOperatorLexeme,
            left.GetType().Name,
            right.GetType().Name);
    }

    public static object? LeftShift(object left, object right)
    {
        var shiftAmount = Convert.ToInt32(right);

        // ECMA-334 §12.11: Unary numeric promotion is applied to the left operand.
        if (left is char c) left = (int)c;

        return left switch
        {
            int i => i << shiftAmount,
            long l => l << shiftAmount,
            uint u => u << shiftAmount,
            ulong ul => ul << shiftAmount,
            short s => s << shiftAmount,
            ushort us => us << shiftAmount,
            byte b => b << shiftAmount,
            sbyte sb => sb << shiftAmount,
            _ => throw new CsEvalException(DiagnosticDescriptors.BadBinaryOps, "<<", left.GetType().Name, right.GetType().Name)
        };
    }

    public static object? RightShift(object left, object right)
    {
        var shiftAmount = Convert.ToInt32(right);

        // ECMA-334 §12.11: Unary numeric promotion is applied to the left operand.
        if (left is char c) left = (int)c;

        return left switch
        {
            int i => i >> shiftAmount,
            long l => l >> shiftAmount,
            uint u => u >> shiftAmount,
            ulong ul => ul >> shiftAmount,
            short s => s >> shiftAmount,
            ushort us => us >> shiftAmount,
            byte b => b >> shiftAmount,
            sbyte sb => sb >> shiftAmount,
            _ => throw new CsEvalException(DiagnosticDescriptors.BadBinaryOps, ">>", left.GetType().Name, right.GetType().Name)
        };
    }

    public static object? UnsignedRightShift(object left, object right)
    {
        var shiftAmount = Convert.ToInt32(right);

        if (left is char c) left = (int)c;

        return left switch
        {
            int i => (int)((uint)i >> (shiftAmount & 0x1F)),
            long l => (long)((ulong)l >> (shiftAmount & 0x3F)),
            uint u => u >> (shiftAmount & 0x1F),
            ulong ul => ul >> (shiftAmount & 0x3F),
            short s => (int)((uint)(int)s >> (shiftAmount & 0x1F)),
            ushort us => us >> (shiftAmount & 0x1F),
            byte b => b >> (shiftAmount & 0x1F),
            sbyte sb => (int)((uint)(int)sb >> (shiftAmount & 0x1F)),
            _ => throw new CsEvalException(DiagnosticDescriptors.BadBinaryOps, ">>>", left.GetType().Name, "int")
        };
    }

    #endregion

    #region Constant Expression Promotion (ECMA-334 §10.2.11)

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
        var leftType = left.GetType();
        var rightType = right.GetType();

        // Case 1: uint op constant-int (value >= 0) -> both uint
        if (leftType == typeof(uint) && rightIsConstant && rightType == typeof(int))
        {
            var intVal = (int)right;
            if (intVal >= 0)
                return (left, (uint)intVal, typeof(uint));
        }

        // Case 2: constant-int (value >= 0) op uint -> both uint
        if (rightType == typeof(uint) && leftIsConstant && leftType == typeof(int))
        {
            var intVal = (int)left;
            if (intVal >= 0)
                return ((uint)intVal, right, typeof(uint));
        }

        // Case 3: ulong op constant-int (value >= 0) -> both ulong
        if (leftType == typeof(ulong) && rightIsConstant && rightType == typeof(int))
        {
            var intVal = (int)right;
            if (intVal >= 0)
                return (left, (ulong)intVal, typeof(ulong));
        }

        // Case 4: constant-int (value >= 0) op ulong -> both ulong
        if (rightType == typeof(ulong) && leftIsConstant && leftType == typeof(int))
        {
            var intVal = (int)left;
            if (intVal >= 0)
                return ((ulong)intVal, right, typeof(ulong));
        }

        // Case 5: ulong op constant-long (value >= 0) -> both ulong
        if (leftType == typeof(ulong) && rightIsConstant && rightType == typeof(long))
        {
            var longVal = (long)right;
            if (longVal >= 0)
                return (left, (ulong)longVal, typeof(ulong));
        }

        // Case 6: constant-long (value >= 0) op ulong -> both ulong
        if (rightType == typeof(ulong) && leftIsConstant && leftType == typeof(long))
        {
            var longVal = (long)left;
            if (longVal >= 0)
                return ((ulong)longVal, right, typeof(ulong));
        }

        return null;
    }

    #endregion

    #region Type Promotion (ECMA-334 §12.4.7.3)

    /// <summary>
    /// Gets the result type for binary numeric operations according to ECMA-334 rules.
    /// Used for static type inference without actual values.
    ///
    /// IMPORTANT: char is NOT eagerly converted to int. Per §10.2.3, char has implicit
    /// conversions to ushort, int, uint, long, ulong, float, double, decimal.
    /// Rule 6 checks for "sbyte, short, or int" -- NOT char.
    /// So uint + char -> uint (Rule 7), not long (Rule 6).
    /// </summary>
    public static Type GetResultType(Type leftType, Type rightType)
    {
        // Rule 1: decimal (no float/double mixing at compile time)
        if (leftType == typeof(decimal) || rightType == typeof(decimal))
            return typeof(decimal);

        // Rule 2: double
        if (leftType == typeof(double) || rightType == typeof(double))
            return typeof(double);

        // Rule 3: float
        if (leftType == typeof(float) || rightType == typeof(float))
            return typeof(float);

        // Rule 4: ulong (error if other operand is a signed integer type)
        // Note: char is NOT signed, so char -> ulong is valid per §10.2.3
        if (leftType == typeof(ulong) || rightType == typeof(ulong))
        {
            if (IsSignedInteger(leftType) || IsSignedInteger(rightType))
                throw new CsEvalException(DiagnosticDescriptors.BadBinaryOps, "+", leftType.Name, rightType.Name);
            return typeof(ulong);
        }

        // Rule 5: long
        if (leftType == typeof(long) || rightType == typeof(long))
            return typeof(long);

        // Rule 6: uint + signed -> long
        // Note: char is NOT sbyte, short, or int
        if ((leftType == typeof(uint) && IsSignedInteger(rightType)) ||
            (rightType == typeof(uint) && IsSignedInteger(leftType)))
            return typeof(long);

        // Rule 7: uint (char -> uint is a valid implicit conversion per §10.2.3)
        if (leftType == typeof(uint) || rightType == typeof(uint))
            return typeof(uint);

        // Rule 8: default to int (includes byte, sbyte, short, ushort, char)
        return typeof(int);
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
                throw new CsEvalException(DiagnosticDescriptors.BadBinaryOps, "+", leftType.Name, rightType.Name);
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
                throw new CsEvalException(DiagnosticDescriptors.BadBinaryOps, "+", leftType.Name, rightType.Name);
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

        return targetType.Name switch
        {
            "Int64" => Convert.ToInt64(value),
            "Double" => Convert.ToDouble(value),
            "Single" => Convert.ToSingle(value),
            "Decimal" => Convert.ToDecimal(value),
            "UInt64" => Convert.ToUInt64(value),
            "UInt32" => Convert.ToUInt32(value),
            "Int32" => Convert.ToInt32(value),
            _ => value
        };
    }

    #endregion

    #region Builder Helpers

    private static object ExecuteBinaryOp(
        object left, object right,
        FixedDictionary<(Type, Type), BinaryOp> ops,
        string opName)
    {
        var (promotedLeft, promotedRight, resultType) = PromoteOperands(left, right);
        var key = (resultType, resultType);

        if (ops.TryGetValue(key, out var op))
            return op(promotedLeft, promotedRight);

        throw new CsEvalException(DiagnosticDescriptors.BadBinaryOps, opName, left.GetType().Name, right.GetType().Name);
    }

    private static object ExecuteIntegerBinaryOp(
        object left, object right,
        FixedDictionary<(Type, Type), BinaryOp> ops,
        string opName)
    {
        var (promotedLeft, promotedRight, resultType) = PromoteOperands(left, right);
        var key = (resultType, resultType);

        if (ops.TryGetValue(key, out var op))
            return op(promotedLeft, promotedRight);

        throw new CsEvalException(DiagnosticDescriptors.BadBinaryOps, opName, left.GetType().Name, right.GetType().Name);
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

    #endregion
}
