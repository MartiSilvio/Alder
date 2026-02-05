namespace CsEval.Runtime;

/// <summary>
/// Fast numeric operator dispatch without dynamic.
/// Implements ECMA-334 §12.4.7.3 binary numeric promotion rules.
/// </summary>
public static class NumericDispatch
{
    public delegate object BinaryOp(object left, object right);
    public delegate object UnaryOp(object value);
    public delegate int CompareOp(object left, object right);

    #region Binary Operator Delegates

    private static readonly Dictionary<(Type, Type), BinaryOp> AddOps = BuildBinaryOps(
        (int l, int r) => l + r,
        (long l, long r) => l + r,
        (float l, float r) => l + r,
        (double l, double r) => l + r,
        (decimal l, decimal r) => l + r,
        (uint l, uint r) => l + r,
        (ulong l, ulong r) => l + r
    );

    private static readonly Dictionary<(Type, Type), BinaryOp> SubtractOps = BuildBinaryOps(
        (int l, int r) => l - r,
        (long l, long r) => l - r,
        (float l, float r) => l - r,
        (double l, double r) => l - r,
        (decimal l, decimal r) => l - r,
        (uint l, uint r) => l - r,
        (ulong l, ulong r) => l - r
    );

    private static readonly Dictionary<(Type, Type), BinaryOp> MultiplyOps = BuildBinaryOps(
        (int l, int r) => l * r,
        (long l, long r) => l * r,
        (float l, float r) => l * r,
        (double l, double r) => l * r,
        (decimal l, decimal r) => l * r,
        (uint l, uint r) => l * r,
        (ulong l, ulong r) => l * r
    );

    private static readonly Dictionary<(Type, Type), BinaryOp> DivideOps = BuildBinaryOps(
        (int l, int r) => l / r,
        (long l, long r) => l / r,
        (float l, float r) => l / r,
        (double l, double r) => l / r,
        (decimal l, decimal r) => l / r,
        (uint l, uint r) => l / r,
        (ulong l, ulong r) => l / r
    );

    private static readonly Dictionary<(Type, Type), BinaryOp> ModuloOps = BuildBinaryOps(
        (int l, int r) => l % r,
        (long l, long r) => l % r,
        (float l, float r) => l % r,
        (double l, double r) => l % r,
        (decimal l, decimal r) => l % r,
        (uint l, uint r) => l % r,
        (ulong l, ulong r) => l % r
    );

    private static readonly Dictionary<(Type, Type), BinaryOp> BitwiseAndOps = BuildIntegerBinaryOps(
        (int l, int r) => l & r,
        (long l, long r) => l & r,
        (uint l, uint r) => l & r,
        (ulong l, ulong r) => l & r
    );

    private static readonly Dictionary<(Type, Type), BinaryOp> BitwiseOrOps = BuildIntegerBinaryOps(
        (int l, int r) => l | r,
        (long l, long r) => l | r,
        (uint l, uint r) => l | r,
        (ulong l, ulong r) => l | r
    );

    private static readonly Dictionary<(Type, Type), BinaryOp> BitwiseXorOps = BuildIntegerBinaryOps(
        (int l, int r) => l ^ r,
        (long l, long r) => l ^ r,
        (uint l, uint r) => l ^ r,
        (ulong l, ulong r) => l ^ r
    );

    #endregion

    #region Comparison Delegates

    private static readonly Dictionary<(Type, Type), CompareOp> CompareOps = BuildCompareOps();

    #endregion

    #region Unary Operator Delegates

    private static readonly Dictionary<Type, UnaryOp> NegateOps = new()
    {
        [typeof(int)] = v => -(int)v,
        [typeof(long)] = v => -(long)v,
        [typeof(float)] = v => -(float)v,
        [typeof(double)] = v => -(double)v,
        [typeof(decimal)] = v => -(decimal)v,
        [typeof(short)] = v => -(short)v,
        [typeof(sbyte)] = v => -(sbyte)v,
    };

    private static readonly Dictionary<Type, UnaryOp> BitwiseNotOps = new()
    {
        [typeof(int)] = v => ~(int)v,
        [typeof(long)] = v => ~(long)v,
        [typeof(uint)] = v => ~(uint)v,
        [typeof(ulong)] = v => ~(ulong)v,
        [typeof(short)] = v => ~(short)v,
        [typeof(ushort)] = v => ~(ushort)v,
        [typeof(byte)] = v => ~(byte)v,
        [typeof(sbyte)] = v => ~(sbyte)v,
    };

    #endregion

    #region Public API

    public static object? Add(object left, object right)
        => ExecuteBinaryOp(left, right, AddOps, "+");

    public static object? Subtract(object left, object right)
        => ExecuteBinaryOp(left, right, SubtractOps, "-");

    public static object? Multiply(object left, object right)
        => ExecuteBinaryOp(left, right, MultiplyOps, "*");

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

    public static object? Negate(object value)
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

        // Handle ulong → long edge case for long.MinValue
        if (type == typeof(ulong))
        {
            var ulongVal = (ulong)value;
            if (ulongVal == (ulong)long.MaxValue + 1)
                return long.MinValue;
        }

        if (NegateOps.TryGetValue(type, out var op))
            return op(value);

#if USE_STATIC_DISPATCH
        throw new CsEvalException($"Cannot negate {type.Name} (unsupported type)");
#else
        return -(dynamic)value;
#endif
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

        if (BitwiseNotOps.TryGetValue(type, out var op))
            return op(value);

#if USE_STATIC_DISPATCH
        throw new CsEvalException($"Cannot apply bitwise NOT to {type.Name} (unsupported type)");
#else
        return ~(dynamic)value;
#endif
    }

    public static int Compare(object left, object right)
    {
        var (promotedLeft, promotedRight, resultType) = PromoteOperands(left, right);
        var key = (resultType, resultType);

        if (CompareOps.TryGetValue(key, out var op))
            return op(promotedLeft, promotedRight);

#if USE_STATIC_DISPATCH
        throw new CsEvalException($"Cannot compare {left.GetType().Name} and {right.GetType().Name} (unsupported types)");
#else
        dynamic l = left, r = right;
        return l < r ? -1 : l > r ? 1 : 0;
#endif
    }

    public static object? LeftShift(object left, object right)
    {
        var shiftAmount = Convert.ToInt32(right);

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
#if USE_STATIC_DISPATCH
            _ => throw new CsEvalException($"Cannot left shift {left.GetType().Name} (unsupported type)")
#else
            _ => (dynamic)left << shiftAmount
#endif
        };
    }

    public static object? RightShift(object left, object right)
    {
        var shiftAmount = Convert.ToInt32(right);

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
#if USE_STATIC_DISPATCH
            _ => throw new CsEvalException($"Cannot right shift {left.GetType().Name} (unsupported type)")
#else
            _ => (dynamic)left >> shiftAmount
#endif
        };
    }

    #endregion

    #region Type Promotion (ECMA-334 §12.4.7.3)

    /// <summary>
    /// Gets the result type for binary numeric operations according to ECMA-334 rules.
    /// Used for static type inference without actual values.
    /// </summary>
    public static Type GetResultType(Type leftType, Type rightType)
    {
        // Handle char as int
        if (leftType == typeof(char)) leftType = typeof(int);
        if (rightType == typeof(char)) rightType = typeof(int);

        // Rule 1: decimal (no float/double mixing at compile time)
        if (leftType == typeof(decimal) || rightType == typeof(decimal))
            return typeof(decimal);

        // Rule 2: double
        if (leftType == typeof(double) || rightType == typeof(double))
            return typeof(double);

        // Rule 3: float
        if (leftType == typeof(float) || rightType == typeof(float))
            return typeof(float);

        // Rule 4: ulong
        if (leftType == typeof(ulong) || rightType == typeof(ulong))
            return typeof(ulong);

        // Rule 5: long
        if (leftType == typeof(long) || rightType == typeof(long))
            return typeof(long);

        // Rule 6: uint + signed → long
        if ((leftType == typeof(uint) && IsSignedInteger(rightType)) ||
            (rightType == typeof(uint) && IsSignedInteger(leftType)))
            return typeof(long);

        // Rule 7: uint
        if (leftType == typeof(uint) || rightType == typeof(uint))
            return typeof(uint);

        // Rule 8: default to int (includes byte, short, sbyte, ushort)
        return typeof(int);
    }

    /// <summary>
    /// Promotes two operands according to ECMA-334 binary numeric promotion rules.
    /// Returns the promoted values and the result type.
    /// </summary>
    public static (object Left, object Right, Type ResultType) PromoteOperands(object left, object right)
    {
        var leftType = left.GetType();
        var rightType = right.GetType();

        // Handle char as int
        if (leftType == typeof(char)) { left = (int)(char)left; leftType = typeof(int); }
        if (rightType == typeof(char)) { right = (int)(char)right; rightType = typeof(int); }

        // Rule 1: decimal (error if mixed with float/double)
        if (leftType == typeof(decimal) || rightType == typeof(decimal))
        {
            if (leftType == typeof(float) || leftType == typeof(double) ||
                rightType == typeof(float) || rightType == typeof(double))
            {
                throw new CsEvalException("Cannot mix decimal with float or double (C# forbids implicit conversion)");
            }
            return (Convert.ToDecimal(left), Convert.ToDecimal(right), typeof(decimal));
        }

        // Rule 2: double
        if (leftType == typeof(double) || rightType == typeof(double))
            return (Convert.ToDouble(left), Convert.ToDouble(right), typeof(double));

        // Rule 3: float
        if (leftType == typeof(float) || rightType == typeof(float))
            return (Convert.ToSingle(left), Convert.ToSingle(right), typeof(float));

        // Rule 4: ulong (error if mixed with signed types)
        if (leftType == typeof(ulong) || rightType == typeof(ulong))
        {
            if (IsSignedInteger(leftType) || IsSignedInteger(rightType))
            {
                throw new CsEvalException("Cannot mix ulong with signed integer types");
            }
            return (Convert.ToUInt64(left), Convert.ToUInt64(right), typeof(ulong));
        }

        // Rule 5: long
        if (leftType == typeof(long) || rightType == typeof(long))
            return (Convert.ToInt64(left), Convert.ToInt64(right), typeof(long));

        // Rule 6: uint + signed → long
        if ((leftType == typeof(uint) && IsSignedInteger(rightType)) ||
            (rightType == typeof(uint) && IsSignedInteger(leftType)))
            return (Convert.ToInt64(left), Convert.ToInt64(right), typeof(long));

        // Rule 7: uint
        if (leftType == typeof(uint) || rightType == typeof(uint))
            return (Convert.ToUInt32(left), Convert.ToUInt32(right), typeof(uint));

        // Rule 8: default to int
        return (Convert.ToInt32(left), Convert.ToInt32(right), typeof(int));
    }

    private static bool IsSignedInteger(Type type) =>
        type == typeof(sbyte) || type == typeof(short) || type == typeof(int) || type == typeof(long);

    #endregion

    #region Builder Helpers

    private static object ExecuteBinaryOp(
        object left, object right,
        Dictionary<(Type, Type), BinaryOp> ops,
        string opName)
    {
        var (promotedLeft, promotedRight, resultType) = PromoteOperands(left, right);
        var key = (resultType, resultType);

        if (ops.TryGetValue(key, out var op))
            return op(promotedLeft, promotedRight);

#if USE_STATIC_DISPATCH
        throw new CsEvalException($"Cannot apply operator '{opName}' to {left.GetType().Name} and {right.GetType().Name} (unsupported types)");
#else
        return opName switch
        {
            "+" => (dynamic)left + (dynamic)right,
            "-" => (dynamic)left - (dynamic)right,
            "*" => (dynamic)left * (dynamic)right,
            "/" => (dynamic)left / (dynamic)right,
            "%" => (dynamic)left % (dynamic)right,
            _ => throw new CsEvalException($"Unknown operator '{opName}'")
        };
#endif
    }

    private static object ExecuteIntegerBinaryOp(
        object left, object right,
        Dictionary<(Type, Type), BinaryOp> ops,
        string opName)
    {
        var leftType = left.GetType();
        var rightType = right.GetType();

        // Handle char as int for bitwise ops
        if (leftType == typeof(char)) { left = (int)(char)left; leftType = typeof(int); }
        if (rightType == typeof(char)) { right = (int)(char)right; rightType = typeof(int); }

        // Determine result type for integer operations
        Type resultType;
        if (leftType == typeof(ulong) || rightType == typeof(ulong))
            resultType = typeof(ulong);
        else if (leftType == typeof(long) || rightType == typeof(long))
            resultType = typeof(long);
        else if (leftType == typeof(uint) || rightType == typeof(uint))
            resultType = typeof(uint);
        else
            resultType = typeof(int);

        object promotedLeft = ConvertToIntegerType(left, resultType);
        object promotedRight = ConvertToIntegerType(right, resultType);

        var key = (resultType, resultType);

        if (ops.TryGetValue(key, out var op))
            return op(promotedLeft, promotedRight);

#if USE_STATIC_DISPATCH
        throw new CsEvalException($"Cannot apply operator '{opName}' to {left.GetType().Name} and {right.GetType().Name} (unsupported types)");
#else
        return opName switch
        {
            "&" => (dynamic)left & (dynamic)right,
            "|" => (dynamic)left | (dynamic)right,
            "^" => (dynamic)left ^ (dynamic)right,
            _ => throw new CsEvalException($"Unknown operator '{opName}'")
        };
#endif
    }

    private static object ConvertToIntegerType(object value, Type targetType)
    {
        if (targetType == typeof(int)) return Convert.ToInt32(value);
        if (targetType == typeof(long)) return Convert.ToInt64(value);
        if (targetType == typeof(uint)) return Convert.ToUInt32(value);
        if (targetType == typeof(ulong)) return Convert.ToUInt64(value);
        return value;
    }

    private static Dictionary<(Type, Type), BinaryOp> BuildBinaryOps(
        Func<int, int, int> intOp,
        Func<long, long, long> longOp,
        Func<float, float, float> floatOp,
        Func<double, double, double> doubleOp,
        Func<decimal, decimal, decimal> decimalOp,
        Func<uint, uint, uint> uintOp,
        Func<ulong, ulong, ulong> ulongOp)
    {
        return new Dictionary<(Type, Type), BinaryOp>
        {
            [(typeof(int), typeof(int))] = (l, r) => intOp((int)l, (int)r),
            [(typeof(long), typeof(long))] = (l, r) => longOp((long)l, (long)r),
            [(typeof(float), typeof(float))] = (l, r) => floatOp((float)l, (float)r),
            [(typeof(double), typeof(double))] = (l, r) => doubleOp((double)l, (double)r),
            [(typeof(decimal), typeof(decimal))] = (l, r) => decimalOp((decimal)l, (decimal)r),
            [(typeof(uint), typeof(uint))] = (l, r) => uintOp((uint)l, (uint)r),
            [(typeof(ulong), typeof(ulong))] = (l, r) => ulongOp((ulong)l, (ulong)r),
        };
    }

    private static Dictionary<(Type, Type), BinaryOp> BuildIntegerBinaryOps(
        Func<int, int, int> intOp,
        Func<long, long, long> longOp,
        Func<uint, uint, uint> uintOp,
        Func<ulong, ulong, ulong> ulongOp)
    {
        return new Dictionary<(Type, Type), BinaryOp>
        {
            [(typeof(int), typeof(int))] = (l, r) => intOp((int)l, (int)r),
            [(typeof(long), typeof(long))] = (l, r) => longOp((long)l, (long)r),
            [(typeof(uint), typeof(uint))] = (l, r) => uintOp((uint)l, (uint)r),
            [(typeof(ulong), typeof(ulong))] = (l, r) => ulongOp((ulong)l, (ulong)r),
        };
    }

    private static Dictionary<(Type, Type), CompareOp> BuildCompareOps()
    {
        return new Dictionary<(Type, Type), CompareOp>
        {
            [(typeof(int), typeof(int))] = (l, r) => ((int)l).CompareTo((int)r),
            [(typeof(long), typeof(long))] = (l, r) => ((long)l).CompareTo((long)r),
            [(typeof(float), typeof(float))] = (l, r) => ((float)l).CompareTo((float)r),
            [(typeof(double), typeof(double))] = (l, r) => ((double)l).CompareTo((double)r),
            [(typeof(decimal), typeof(decimal))] = (l, r) => ((decimal)l).CompareTo((decimal)r),
            [(typeof(uint), typeof(uint))] = (l, r) => ((uint)l).CompareTo((uint)r),
            [(typeof(ulong), typeof(ulong))] = (l, r) => ((ulong)l).CompareTo((ulong)r),
        };
    }

    #endregion
}
