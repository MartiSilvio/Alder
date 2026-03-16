using System.Collections.Concurrent;
using CsEval.Runtime.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CsEval.Diagnostics;

namespace CsEval.Runtime;

/// <summary>
/// Type checking, validation, and conversion utilities.
/// </summary>
internal static class TypeHelpers
{
    public static int GetSizeOf(string typeName) => typeName switch
    {
        "bool" or "Boolean" or "System.Boolean" => 1,
        "byte" or "Byte" or "System.Byte" => 1,
        "sbyte" or "SByte" or "System.SByte" => 1,
        "char" or "Char" or "System.Char" => 2,
        "short" or "Int16" or "System.Int16" => 2,
        "ushort" or "UInt16" or "System.UInt16" => 2,
        "int" or "Int32" or "System.Int32" => 4,
        "uint" or "UInt32" or "System.UInt32" => 4,
        "float" or "Single" or "System.Single" => 4,
        "long" or "Int64" or "System.Int64" => 8,
        "ulong" or "UInt64" or "System.UInt64" => 8,
        "double" or "Double" or "System.Double" => 8,
        "decimal" or "Decimal" or "System.Decimal" => 16,
        _ => throw new CsEvalException(DiagnosticDescriptors.SizeofUnsupportedType, typeName)
    };

    public static bool RequireBoolean(object? value)
    {
        if (value is bool b)
            return b;

        throw new CsEvalException(DiagnosticDescriptors.NoImplicitConversion, TypeNameFormatter.Of(value), "bool");
    }

    public static bool RequireBooleanForLogicalOperator(object? value, string opLexeme, string otherOperandTypeName)
    {
        if (value is bool b)
            return b;

        throw new CsEvalException(
            DiagnosticDescriptors.BadBinaryOps,
            opLexeme,
            TypeNameFormatter.Of(value),
            otherOperandTypeName);
    }

    // §12.13.5 + §12.14.2: Three-value logic for bool? && and bool? ||
    public static object? NullableBoolAnd(object? left, object? right)
    {
        var l = left as bool?;
        var r = right as bool?;
        if (l == false || r == false) return false;
        if (l == true && r == true) return true;
        return null;
    }

    public static object? NullableBoolOr(object? left, object? right)
    {
        var l = left as bool?;
        var r = right as bool?;
        if (l == true || r == true) return true;
        if (l == false && r == false) return false;
        return null;
    }

    internal static bool IsInteger([NotNullWhen(true)] object? value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong;

    internal static bool IsNumeric([NotNullWhen(true)] object? value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;

    /// <summary>
    /// Checks if a value can participate in arithmetic operations.
    /// Per ECMA-334 §12.4.7.2, char undergoes unary numeric promotion to int.
    /// </summary>
    internal static bool IsArithmetic([NotNullWhen(true)] object? value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal or char;

    internal static bool IsArithmetic(Type type) =>
        Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.Decimal or TypeCode.Char;

    private static bool IsIntegerType(Type type) =>
        Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.UInt64;

    private static bool IsNumericOrCharType(Type type) =>
        Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.Decimal or TypeCode.Char;

    private static bool IsConstantIntConversionTarget(Type type) =>
        type == typeof(sbyte) || type == typeof(byte) ||
        type == typeof(short) || type == typeof(ushort) ||
        type == typeof(uint) || type == typeof(ulong) ||
        type == typeof(char);

    /// <summary>
    /// ECMA-334 §10.2.3: Implicit numeric conversions.
    /// "There are no predefined implicit conversions to the char type, so values of the
    /// other integral types do not automatically convert to the char type." (§10.2.3)
    /// </summary>
    private static readonly FixedDictionary<Type, FixedSet<Type>> ImplicitConversions = FixedDictionary<Type, FixedSet<Type>>.Create(
        new Dictionary<Type, HashSet<Type>>
        {
            [typeof(sbyte)] = [typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
            [typeof(byte)] = [typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
            [typeof(short)] = [typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
            [typeof(ushort)] = [typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
            [typeof(int)] = [typeof(long), typeof(float), typeof(double), typeof(decimal)],
            [typeof(uint)] = [typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
            [typeof(long)] = [typeof(float), typeof(double), typeof(decimal)],
            [typeof(ulong)] = [typeof(float), typeof(double), typeof(decimal)],
            [typeof(float)] = [typeof(double)],
            [typeof(char)] = [typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
        },
        kvp => kvp.Key,
        kvp => FixedSet<Type>.Create(kvp.Value));

    /// <summary>
    /// Returns the default value for a type (ECMA-334 §12.8.20).
    /// For value types, returns the zero/false/null equivalent.
    /// For reference types and nullable types, returns null.
    /// </summary>
    public static object? GetDefaultValue(Type type)
    {
        if (!type.IsValueType)
            return null;

        if (Nullable.GetUnderlyingType(type) != null)
            return null;

        if (type.IsEnum)
            return Enum.ToObject(type, 0);

        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => false,
            TypeCode.Char => '\0',
            TypeCode.SByte => (sbyte)0,
            TypeCode.Byte => (byte)0,
            TypeCode.Int16 => (short)0,
            TypeCode.UInt16 => (ushort)0,
            TypeCode.Int32 => 0,
            TypeCode.UInt32 => 0u,
            TypeCode.Int64 => 0L,
            TypeCode.UInt64 => 0UL,
            TypeCode.Single => 0f,
            TypeCode.Double => 0d,
            TypeCode.Decimal => 0m,
            TypeCode.DateTime => default(DateTime),
#if NET5_0_OR_GREATER
            _ => System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type)
#else
            _ => System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type)
#endif
        };
    }

    /// <summary>
    /// Performs an explicit cast with optional static type checking.
    /// When sourceStaticType is 'object', enforces C# unboxing semantics:
    /// you can only unbox to the exact boxed type.
    /// </summary>
    public static object? ExplicitCast(object? value, Type targetType, Type? sourceStaticType = null, bool isChecked = false)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var isNullable = Nullable.GetUnderlyingType(targetType) != null;

        if (value == null)
        {
            if (targetType.IsValueType && !isNullable)
                throw new CsEvalException(DiagnosticDescriptors.NullToNonNullable, targetType.Name);
            return null;
        }

        var runtimeType = value.GetType();

        // Same type - no conversion needed
        if (runtimeType == underlyingType || runtimeType == targetType)
            return value;

        // Handle reference types (string, object)
        if (underlyingType == typeof(string))
        {
            if (value is char c)
                return c.ToString();
            throw new CsEvalException(DiagnosticDescriptors.NoExplicitConversion, runtimeType.Name, "String");
        }

        if (underlyingType == typeof(object))
            return value;

        // Reference type cast: check assignability for non-value-type targets
        if (!underlyingType.IsValueType && underlyingType != typeof(string))
        {
            if (underlyingType.IsAssignableFrom(runtimeType))
                return value;
            throw new CsEvalException(DiagnosticDescriptors.NoExplicitConversion, runtimeType.Name, underlyingType.Name);
        }

        // C# unboxing rule: when source static type is 'object', you can only unbox to the exact boxed type
        // (long)(object)42 fails because 42 is boxed as int, not long
        if (sourceStaticType == typeof(object) && underlyingType.IsValueType && runtimeType != underlyingType)
        {
            throw new CsEvalException(DiagnosticDescriptors.NoExplicitConversion, runtimeType.Name, underlyingType.Name);
        }

        // Numeric and char conversions
        try
        {
            if (underlyingType == typeof(char) && value is string { Length: 1 } s)
                return s[0];

            return RuntimeCast(value, runtimeType, underlyingType, isChecked);
        }
        catch (OverflowException) when (isChecked)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            throw new CsEvalException(DiagnosticDescriptors.NoExplicitConversion, runtimeType.Name, targetType.Name);
        }
    }

    private static readonly ConcurrentDictionary<(Type SourceType, Type TargetType, bool IsChecked), Func<object, object>> CastCache = new();
    private static readonly ConcurrentQueue<(Type, Type, bool)> _castInsertionOrder = new();
    private const int MaxCastCacheSize = 2048;

    internal static object RuntimeCast(object value, Type sourceType, Type targetType, bool isChecked = false)
    {
        var key = (sourceType, targetType, isChecked);
        if (CastCache.TryGetValue(key, out var existing))
            return existing(value);

        var converter = CreateCastConverter(sourceType, targetType, isChecked);
        if (CastCache.TryAdd(key, converter))
        {
            _castInsertionOrder.Enqueue(key);
            while (CastCache.Count > MaxCastCacheSize && _castInsertionOrder.TryDequeue(out var oldest))
                CastCache.TryRemove(oldest, out _);
        }
        return converter(value);
    }

    private static Func<object, object> CreateCastConverter(Type sourceType, Type targetType, bool isChecked)
    {
        if (sourceType == targetType)
            return static value => value;

        if (TryCreateEnumCastConverter(sourceType, targetType, isChecked, out var enumConverter))
            return enumConverter;

        if (TryGetRuntimeNumericTypeCode(sourceType, out var sourceCode) &&
            TryGetRuntimeNumericTypeCode(targetType, out var targetCode))
        {
            return value => ConvertPrimitive(value, sourceCode, targetCode, isChecked);
        }

        if (TryResolveUserDefinedConversion(sourceType, targetType, out var userDefinedMethod))
            return value => userDefinedMethod.Invoke(null, [value])!;

        return _ => throw new InvalidCastException(
            $"No explicit conversion exists from '{sourceType.Name}' to '{targetType.Name}'.");
    }

    private static bool TryCreateEnumCastConverter(
        Type sourceType,
        Type targetType,
        bool isChecked,
        [NotNullWhen(true)] out Func<object, object>? converter)
    {
        converter = null;

        if (!sourceType.IsEnum && !targetType.IsEnum)
            return false;

        if (sourceType.IsEnum)
        {
            var sourceUnderlying = Enum.GetUnderlyingType(sourceType);
            if (!TryGetRuntimeNumericTypeCode(sourceUnderlying, out var sourceCode))
                return false;

            if (targetType.IsEnum)
            {
                var targetUnderlying = Enum.GetUnderlyingType(targetType);
                if (!TryGetRuntimeNumericTypeCode(targetUnderlying, out var targetCode))
                    return false;

                converter = value =>
                {
                    var underlyingValue = GetEnumUnderlyingValue(value, sourceCode);
                    var converted = ConvertPrimitive(underlyingValue, sourceCode, targetCode, isChecked);
                    return Enum.ToObject(targetType, converted);
                };
                return true;
            }

            if (!TryGetRuntimeNumericTypeCode(targetType, out var numericTargetCode))
                return false;

            converter = value =>
            {
                var underlyingValue = GetEnumUnderlyingValue(value, sourceCode);
                return ConvertPrimitive(underlyingValue, sourceCode, numericTargetCode, isChecked);
            };
            return true;
        }

        if (!TryGetRuntimeNumericTypeCode(sourceType, out var numericSourceCode))
            return false;

        var enumUnderlyingType = Enum.GetUnderlyingType(targetType);
        if (!TryGetRuntimeNumericTypeCode(enumUnderlyingType, out var enumTargetCode))
            return false;

        converter = value =>
        {
            var converted = ConvertPrimitive(value, numericSourceCode, enumTargetCode, isChecked);
            return Enum.ToObject(targetType, converted);
        };
        return true;
    }

    /// <summary>
    /// ECMA-334 §10.5.3–§10.5.5: Find the most specific user-defined conversion operator.
    /// Searches source/target type hierarchies, filters by standard conversion applicability,
    /// then selects the most specific source type (Sx) and target type (Tx).
    /// </summary>
    private static bool TryResolveUserDefinedConversion(
        Type sourceType,
        Type targetType,
        [NotNullWhen(true)] out MethodInfo? conversionMethod)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        conversionMethod = null;

        // §10.5.3: Search source type + base classes, and target type + base classes
        var searchTypes = sourceType == targetType
            ? new[] { sourceType }
            : new[] { sourceType, targetType };

        var candidates = new List<MethodInfo>();

        foreach (var declaringType in searchTypes)
        {
            foreach (var method in ReflectionRuntime.GetMethods(declaringType, flags))
            {
                if (method.Name is not ("op_Explicit" or "op_Implicit"))
                    continue;

                var parameters = method.GetParameters();
                if (parameters.Length != 1)
                    continue;

                var paramType = parameters[0].ParameterType;
                var returnType = method.ReturnType;

                // §10.5.5 (explicit): operator applicable if standard conversion exists
                // from sourceType → paramType OR paramType → sourceType (encompassing or encompassed)
                var sourceApplicable = paramType == sourceType
                    || CanImplicitlyConvert(sourceType, paramType)
                    || CanImplicitlyConvert(paramType, sourceType)
                    || (!paramType.IsValueType && paramType.IsAssignableFrom(sourceType))
                    || (!sourceType.IsValueType && sourceType.IsAssignableFrom(paramType));

                // §10.5.5: standard conversion from returnType → targetType or targetType → returnType
                var targetApplicable = returnType == targetType
                    || CanImplicitlyConvert(returnType, targetType)
                    || CanImplicitlyConvert(targetType, returnType)
                    || (!targetType.IsValueType && targetType.IsAssignableFrom(returnType))
                    || (!returnType.IsValueType && returnType.IsAssignableFrom(targetType));

                if (sourceApplicable && targetApplicable)
                    candidates.Add(method);
            }
        }

        if (candidates.Count == 0)
            return false;

        if (candidates.Count == 1)
        {
            conversionMethod = candidates[0];
            return true;
        }

        // §10.5.4/§10.5.5: Find most specific source type Sx
        // Prefer exact source match, then most-encompassed (most specific) type
        conversionMethod = SelectMostSpecific(candidates, sourceType, targetType);
        return conversionMethod != null;
    }

    private static MethodInfo? SelectMostSpecific(List<MethodInfo> candidates, Type sourceType, Type targetType)
    {
        // Prefer exact source type match
        var exactSource = candidates.Where(m => m.GetParameters()[0].ParameterType == sourceType).ToList();
        if (exactSource.Count > 0)
            candidates = exactSource;

        // Prefer exact target type match
        var exactTarget = candidates.Where(m => m.ReturnType == targetType).ToList();
        if (exactTarget.Count > 0)
            candidates = exactTarget;

        if (candidates.Count == 1)
            return candidates[0];

        // §10.5.4: Most-encompassed source type (most specific — the one
        // that all other candidate source types can convert TO)
        MethodInfo? best = null;
        foreach (var candidate in candidates)
        {
            var cParamType = candidate.GetParameters()[0].ParameterType;
            var cReturnType = candidate.ReturnType;
            var isMostSpecific = true;

            foreach (var other in candidates)
            {
                if (ReferenceEquals(candidate, other))
                    continue;

                var oParamType = other.GetParameters()[0].ParameterType;
                var oReturnType = other.ReturnType;

                // Source: candidate is more specific if other's param encompasses candidate's param
                if (cParamType != oParamType && !CanImplicitlyConvert(cParamType, oParamType))
                {
                    isMostSpecific = false;
                    break;
                }

                // Target: candidate is more specific if candidate's return encompasses other's return
                if (cReturnType != oReturnType && !CanImplicitlyConvert(oReturnType, cReturnType))
                {
                    isMostSpecific = false;
                    break;
                }
            }

            if (isMostSpecific)
            {
                if (best != null)
                    return null; // Ambiguous
                best = candidate;
            }
        }

        return best;
    }

    private static bool TryGetRuntimeNumericTypeCode(Type type, out TypeCode typeCode)
    {
        typeCode = Type.GetTypeCode(type);
        return typeCode is
            TypeCode.SByte or
            TypeCode.Byte or
            TypeCode.Int16 or
            TypeCode.UInt16 or
            TypeCode.Int32 or
            TypeCode.UInt32 or
            TypeCode.Int64 or
            TypeCode.UInt64 or
            TypeCode.Char or
            TypeCode.Single or
            TypeCode.Double or
            TypeCode.Decimal;
    }

    private static object GetEnumUnderlyingValue(object value, TypeCode underlyingCode) => underlyingCode switch
    {
        TypeCode.SByte => Convert.ToSByte(value),
        TypeCode.Byte => Convert.ToByte(value),
        TypeCode.Int16 => Convert.ToInt16(value),
        TypeCode.UInt16 => Convert.ToUInt16(value),
        TypeCode.Int32 => Convert.ToInt32(value),
        TypeCode.UInt32 => Convert.ToUInt32(value),
        TypeCode.Int64 => Convert.ToInt64(value),
        TypeCode.UInt64 => Convert.ToUInt64(value),
        _ => throw new InvalidCastException($"Unsupported enum underlying type '{underlyingCode}'.")
    };

    private static object ConvertPrimitive(object value, TypeCode sourceCode, TypeCode targetCode, bool isChecked) => sourceCode switch
    {
        TypeCode.SByte => ConvertFromSByte((sbyte)value, targetCode, isChecked),
        TypeCode.Byte => ConvertFromByte((byte)value, targetCode, isChecked),
        TypeCode.Int16 => ConvertFromInt16((short)value, targetCode, isChecked),
        TypeCode.UInt16 => ConvertFromUInt16((ushort)value, targetCode, isChecked),
        TypeCode.Int32 => ConvertFromInt32((int)value, targetCode, isChecked),
        TypeCode.UInt32 => ConvertFromUInt32((uint)value, targetCode, isChecked),
        TypeCode.Int64 => ConvertFromInt64((long)value, targetCode, isChecked),
        TypeCode.UInt64 => ConvertFromUInt64((ulong)value, targetCode, isChecked),
        TypeCode.Char => ConvertFromChar((char)value, targetCode, isChecked),
        TypeCode.Single => ConvertFromSingle((float)value, targetCode, isChecked),
        TypeCode.Double => ConvertFromDouble((double)value, targetCode, isChecked),
        TypeCode.Decimal => ConvertFromDecimal((decimal)value, targetCode, isChecked),
        _ => throw new InvalidCastException($"No explicit conversion exists from '{sourceCode}' to '{targetCode}'.")
    };

    private static object ConvertFromSByte(sbyte value, TypeCode targetCode, bool isChecked) => targetCode switch
    {
        TypeCode.SByte => value,
        TypeCode.Byte => isChecked ? checked((byte)value) : unchecked((byte)value),
        TypeCode.Int16 => (short)value,
        TypeCode.UInt16 => isChecked ? checked((ushort)value) : unchecked((ushort)value),
        TypeCode.Int32 => (int)value,
        TypeCode.UInt32 => isChecked ? checked((uint)value) : unchecked((uint)value),
        TypeCode.Int64 => (long)value,
        TypeCode.UInt64 => isChecked ? checked((ulong)value) : unchecked((ulong)value),
        TypeCode.Char => isChecked ? checked((char)value) : unchecked((char)value),
        TypeCode.Single => (float)value,
        TypeCode.Double => (double)value,
        TypeCode.Decimal => (decimal)value,
        _ => throw new InvalidCastException($"No explicit conversion exists from '{TypeCode.SByte}' to '{targetCode}'.")
    };

    private static object ConvertFromByte(byte value, TypeCode targetCode, bool isChecked) => targetCode switch
    {
        TypeCode.SByte => isChecked ? checked((sbyte)value) : unchecked((sbyte)value),
        TypeCode.Byte => value,
        TypeCode.Int16 => (short)value,
        TypeCode.UInt16 => (ushort)value,
        TypeCode.Int32 => (int)value,
        TypeCode.UInt32 => (uint)value,
        TypeCode.Int64 => (long)value,
        TypeCode.UInt64 => (ulong)value,
        TypeCode.Char => (char)value,
        TypeCode.Single => (float)value,
        TypeCode.Double => (double)value,
        TypeCode.Decimal => (decimal)value,
        _ => throw new InvalidCastException($"No explicit conversion exists from '{TypeCode.Byte}' to '{targetCode}'.")
    };

    private static object ConvertFromInt16(short value, TypeCode targetCode, bool isChecked) => targetCode switch
    {
        TypeCode.SByte => isChecked ? checked((sbyte)value) : unchecked((sbyte)value),
        TypeCode.Byte => isChecked ? checked((byte)value) : unchecked((byte)value),
        TypeCode.Int16 => value,
        TypeCode.UInt16 => isChecked ? checked((ushort)value) : unchecked((ushort)value),
        TypeCode.Int32 => (int)value,
        TypeCode.UInt32 => isChecked ? checked((uint)value) : unchecked((uint)value),
        TypeCode.Int64 => (long)value,
        TypeCode.UInt64 => isChecked ? checked((ulong)value) : unchecked((ulong)value),
        TypeCode.Char => isChecked ? checked((char)value) : unchecked((char)value),
        TypeCode.Single => (float)value,
        TypeCode.Double => (double)value,
        TypeCode.Decimal => (decimal)value,
        _ => throw new InvalidCastException($"No explicit conversion exists from '{TypeCode.Int16}' to '{targetCode}'.")
    };

    private static object ConvertFromUInt16(ushort value, TypeCode targetCode, bool isChecked) => targetCode switch
    {
        TypeCode.SByte => isChecked ? checked((sbyte)value) : unchecked((sbyte)value),
        TypeCode.Byte => isChecked ? checked((byte)value) : unchecked((byte)value),
        TypeCode.Int16 => isChecked ? checked((short)value) : unchecked((short)value),
        TypeCode.UInt16 => value,
        TypeCode.Int32 => (int)value,
        TypeCode.UInt32 => (uint)value,
        TypeCode.Int64 => (long)value,
        TypeCode.UInt64 => (ulong)value,
        TypeCode.Char => (char)value,
        TypeCode.Single => (float)value,
        TypeCode.Double => (double)value,
        TypeCode.Decimal => (decimal)value,
        _ => throw new InvalidCastException($"No explicit conversion exists from '{TypeCode.UInt16}' to '{targetCode}'.")
    };

    private static object ConvertFromInt32(int value, TypeCode targetCode, bool isChecked) => targetCode switch
    {
        TypeCode.SByte => isChecked ? checked((sbyte)value) : unchecked((sbyte)value),
        TypeCode.Byte => isChecked ? checked((byte)value) : unchecked((byte)value),
        TypeCode.Int16 => isChecked ? checked((short)value) : unchecked((short)value),
        TypeCode.UInt16 => isChecked ? checked((ushort)value) : unchecked((ushort)value),
        TypeCode.Int32 => value,
        TypeCode.UInt32 => isChecked ? checked((uint)value) : unchecked((uint)value),
        TypeCode.Int64 => (long)value,
        TypeCode.UInt64 => isChecked ? checked((ulong)value) : unchecked((ulong)value),
        TypeCode.Char => isChecked ? checked((char)value) : unchecked((char)value),
        TypeCode.Single => (float)value,
        TypeCode.Double => (double)value,
        TypeCode.Decimal => (decimal)value,
        _ => throw new InvalidCastException($"No explicit conversion exists from '{TypeCode.Int32}' to '{targetCode}'.")
    };

    private static object ConvertFromUInt32(uint value, TypeCode targetCode, bool isChecked) => targetCode switch
    {
        TypeCode.SByte => isChecked ? checked((sbyte)value) : unchecked((sbyte)value),
        TypeCode.Byte => isChecked ? checked((byte)value) : unchecked((byte)value),
        TypeCode.Int16 => isChecked ? checked((short)value) : unchecked((short)value),
        TypeCode.UInt16 => isChecked ? checked((ushort)value) : unchecked((ushort)value),
        TypeCode.Int32 => isChecked ? checked((int)value) : unchecked((int)value),
        TypeCode.UInt32 => value,
        TypeCode.Int64 => (long)value,
        TypeCode.UInt64 => (ulong)value,
        TypeCode.Char => isChecked ? checked((char)value) : unchecked((char)value),
        TypeCode.Single => (float)value,
        TypeCode.Double => (double)value,
        TypeCode.Decimal => (decimal)value,
        _ => throw new InvalidCastException($"No explicit conversion exists from '{TypeCode.UInt32}' to '{targetCode}'.")
    };

    private static object ConvertFromInt64(long value, TypeCode targetCode, bool isChecked) => targetCode switch
    {
        TypeCode.SByte => isChecked ? checked((sbyte)value) : unchecked((sbyte)value),
        TypeCode.Byte => isChecked ? checked((byte)value) : unchecked((byte)value),
        TypeCode.Int16 => isChecked ? checked((short)value) : unchecked((short)value),
        TypeCode.UInt16 => isChecked ? checked((ushort)value) : unchecked((ushort)value),
        TypeCode.Int32 => isChecked ? checked((int)value) : unchecked((int)value),
        TypeCode.UInt32 => isChecked ? checked((uint)value) : unchecked((uint)value),
        TypeCode.Int64 => value,
        TypeCode.UInt64 => isChecked ? checked((ulong)value) : unchecked((ulong)value),
        TypeCode.Char => isChecked ? checked((char)value) : unchecked((char)value),
        TypeCode.Single => (float)value,
        TypeCode.Double => (double)value,
        TypeCode.Decimal => (decimal)value,
        _ => throw new InvalidCastException($"No explicit conversion exists from '{TypeCode.Int64}' to '{targetCode}'.")
    };

    private static object ConvertFromUInt64(ulong value, TypeCode targetCode, bool isChecked) => targetCode switch
    {
        TypeCode.SByte => isChecked ? checked((sbyte)value) : unchecked((sbyte)value),
        TypeCode.Byte => isChecked ? checked((byte)value) : unchecked((byte)value),
        TypeCode.Int16 => isChecked ? checked((short)value) : unchecked((short)value),
        TypeCode.UInt16 => isChecked ? checked((ushort)value) : unchecked((ushort)value),
        TypeCode.Int32 => isChecked ? checked((int)value) : unchecked((int)value),
        TypeCode.UInt32 => isChecked ? checked((uint)value) : unchecked((uint)value),
        TypeCode.Int64 => isChecked ? checked((long)value) : unchecked((long)value),
        TypeCode.UInt64 => value,
        TypeCode.Char => isChecked ? checked((char)value) : unchecked((char)value),
        TypeCode.Single => (float)value,
        TypeCode.Double => (double)value,
        TypeCode.Decimal => (decimal)value,
        _ => throw new InvalidCastException($"No explicit conversion exists from '{TypeCode.UInt64}' to '{targetCode}'.")
    };

    private static object ConvertFromChar(char value, TypeCode targetCode, bool isChecked) => targetCode switch
    {
        TypeCode.SByte => isChecked ? checked((sbyte)value) : unchecked((sbyte)value),
        TypeCode.Byte => isChecked ? checked((byte)value) : unchecked((byte)value),
        TypeCode.Int16 => isChecked ? checked((short)value) : unchecked((short)value),
        TypeCode.UInt16 => (ushort)value,
        TypeCode.Int32 => (int)value,
        TypeCode.UInt32 => (uint)value,
        TypeCode.Int64 => (long)value,
        TypeCode.UInt64 => (ulong)value,
        TypeCode.Char => value,
        TypeCode.Single => (float)value,
        TypeCode.Double => (double)value,
        TypeCode.Decimal => (decimal)value,
        _ => throw new InvalidCastException($"No explicit conversion exists from '{TypeCode.Char}' to '{targetCode}'.")
    };

    private static object ConvertFromSingle(float value, TypeCode targetCode, bool isChecked) => targetCode switch
    {
        TypeCode.SByte => isChecked ? checked((sbyte)value) : (sbyte)value,
        TypeCode.Byte => isChecked ? checked((byte)value) : (byte)value,
        TypeCode.Int16 => isChecked ? checked((short)value) : (short)value,
        TypeCode.UInt16 => isChecked ? checked((ushort)value) : (ushort)value,
        TypeCode.Int32 => isChecked ? checked((int)value) : (int)value,
        TypeCode.UInt32 => isChecked ? checked((uint)value) : (uint)value,
        TypeCode.Int64 => isChecked ? checked((long)value) : (long)value,
        TypeCode.UInt64 => isChecked ? checked((ulong)value) : (ulong)value,
        TypeCode.Char => isChecked ? checked((char)value) : (char)value,
        TypeCode.Single => value,
        TypeCode.Double => (double)value,
        TypeCode.Decimal => isChecked ? checked((decimal)value) : (decimal)value,
        _ => throw new InvalidCastException($"No explicit conversion exists from '{TypeCode.Single}' to '{targetCode}'.")
    };

    private static object ConvertFromDouble(double value, TypeCode targetCode, bool isChecked) => targetCode switch
    {
        TypeCode.SByte => isChecked ? checked((sbyte)value) : (sbyte)value,
        TypeCode.Byte => isChecked ? checked((byte)value) : (byte)value,
        TypeCode.Int16 => isChecked ? checked((short)value) : (short)value,
        TypeCode.UInt16 => isChecked ? checked((ushort)value) : (ushort)value,
        TypeCode.Int32 => isChecked ? checked((int)value) : (int)value,
        TypeCode.UInt32 => isChecked ? checked((uint)value) : (uint)value,
        TypeCode.Int64 => isChecked ? checked((long)value) : (long)value,
        TypeCode.UInt64 => isChecked ? checked((ulong)value) : (ulong)value,
        TypeCode.Char => isChecked ? checked((char)value) : (char)value,
        TypeCode.Single => (float)value,
        TypeCode.Double => (double)value,
        TypeCode.Decimal => isChecked ? checked((decimal)value) : (decimal)value,
        _ => throw new InvalidCastException($"No explicit conversion exists from '{TypeCode.Double}' to '{targetCode}'.")
    };

    private static object ConvertFromDecimal(decimal value, TypeCode targetCode, bool isChecked) => targetCode switch
    {
        TypeCode.SByte => isChecked ? checked((sbyte)value) : (sbyte)value,
        TypeCode.Byte => isChecked ? checked((byte)value) : (byte)value,
        TypeCode.Int16 => isChecked ? checked((short)value) : (short)value,
        TypeCode.UInt16 => isChecked ? checked((ushort)value) : (ushort)value,
        TypeCode.Int32 => isChecked ? checked((int)value) : (int)value,
        TypeCode.UInt32 => isChecked ? checked((uint)value) : (uint)value,
        TypeCode.Int64 => isChecked ? checked((long)value) : (long)value,
        TypeCode.UInt64 => isChecked ? checked((ulong)value) : (ulong)value,
        TypeCode.Char => isChecked ? checked((char)value) : (char)value,
        TypeCode.Single => (float)value,
        TypeCode.Double => (double)value,
        TypeCode.Decimal => value,
        _ => throw new InvalidCastException($"No explicit conversion exists from '{TypeCode.Decimal}' to '{targetCode}'.")
    };

    /// <summary>
    /// Converts a numeric value to a target type, handling char specially.
    /// System.Convert.ChangeType does not support char -> float/double/decimal directly,
    /// so we first convert char to ushort (its underlying numeric representation per ECMA-334 §8.3.6)
    /// before calling Convert.ChangeType.
    /// </summary>
    private static object ConvertNumeric(object value, Type sourceType, Type targetType)
    {
        // ECMA-334 §8.3.6: char is a 16-bit unsigned integer (same range as ushort)
        // Convert.ChangeType(char, float/double/decimal) throws InvalidCastException,
        // so convert char to its numeric value first.
        if (sourceType == typeof(char))
            return Convert.ChangeType((ushort)(char)value, targetType);

        return Convert.ChangeType(value, targetType);
    }

    public static bool IsType(object? value, Type targetType)
    {
        if (value == null)
            return false;

        var valueType = value.GetType();
        var underlyingTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return underlyingTarget.IsAssignableFrom(valueType) || valueType == underlyingTarget;
    }

    public static object? TryAs(object? value, Type targetType)
    {
        if (value == null)
            return null;

        var valueType = value.GetType();
        var underlyingTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingTarget.IsAssignableFrom(valueType) || valueType == underlyingTarget)
            return value;

        return null;
    }

    public static bool IsNullableType(Type type)
    {
        if (!type.IsValueType)
            return true;
        return Nullable.GetUnderlyingType(type) != null;
    }

    public static object? ValidateAndCoerceType(Type targetType, object? value, string varName)
    {
        if (targetType == typeof(object))
            return value;

        var isNullable = Nullable.GetUnderlyingType(targetType) != null;
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value == null)
        {
            if (targetType.IsValueType && !isNullable)
                throw new CsEvalException(DiagnosticDescriptors.NullToNonNullable, targetType.Name);
            return null;
        }

        if (value is NamedTupleValue && IsTupleType(underlyingType))
            return value;

        var sourceType = value.GetType();

        if (sourceType == underlyingType || sourceType == targetType)
            return value;

        if (ImplicitConversions.TryGetValue(sourceType, out var allowedTargets) && allowedTargets.Contains(underlyingType))
            return ConvertNumeric(value, sourceType, underlyingType);

        // ECMA-334 §10.2.11: Implicit constant expression conversions
        // "A constant_expression of type int can be converted to type sbyte, byte, short,
        // ushort, uint, or ulong, provided the value of the constant_expression is within
        // the range of the destination type."
        // Note: In CsEval, literal int values in typed declarations arrive here as int values.
        // OverflowException from Convert.ChangeType enforces the range check.
        if (sourceType == typeof(int) && value is int intValue && IsIntegerType(underlyingType) && !underlyingType.IsEnum)
        {
            try { return Convert.ChangeType(intValue, underlyingType); }
            catch (OverflowException)
            {
                throw new CsEvalException(
                    DiagnosticDescriptors.ConstantValueCannotConvert,
                    intValue,
                    underlyingType.Name);
            }
        }

        // §10.2.4: Implicit enumeration conversion — constant 0 → any enum
        // §10.3.3: Explicit enumeration conversion — any integer ↔ enum
        if (underlyingType.IsEnum && IsIntegerType(sourceType))
        {
            var enumUnderlyingType = Enum.GetUnderlyingType(underlyingType);
            var converted = Convert.ChangeType(value, enumUnderlyingType);
            return Enum.ToObject(underlyingType, converted);
        }

        if (underlyingType == typeof(char) && value is string { Length: 1 } s)
            return s[0];

        var delegateInstance = LambdaDelegateConverter.TryConvert(value, underlyingType);
        if (delegateInstance != null)
            return delegateInstance;

        throw CreateImplicitConversionException(sourceType, targetType, value);
    }

    /// <summary>
    /// Checks if a type is a System.ValueTuple generic type.
    /// ECMA-334 §10.2.13, §10.3.6 - Tuple conversions.
    /// </summary>
    public static bool IsTupleType(Type type) =>
        type.IsGenericType && type.FullName?.StartsWith("System.ValueTuple") == true;

    /// <summary>
    /// Checks if a value can be implicitly assigned to a target type per C# rules.
    internal static bool HasUserDefinedImplicitConversion(Type sourceType, Type targetType)
    {
        return TryResolveUserDefinedConversion(sourceType, targetType, out _);
    }

    internal static bool TryApplyUserDefinedImplicitConversion(object value, Type targetType, out object? converted)
    {
        converted = null;
        var sourceType = value.GetType();
        if (!TryResolveUserDefinedConversion(sourceType, targetType, out var method))
            return false;
        converted = method.Invoke(null, [value])!;
        return true;
    }

    /// Handles ECMA-334 §10.2.3 implicit numeric conversions,
    /// ECMA-334 §10.6.1 implicit nullable conversions (T -> T?, S -> T? where S -> T is implicit),
    /// and ECMA-334 §10.2.13 implicit tuple conversions (element-wise implicit convertibility).
    /// Used for assignment validation.
    /// </summary>
    public static bool CanImplicitlyConvert(Type sourceType, Type targetType)
    {
        if (sourceType == targetType)
            return true;

        // ECMA-334 §10.2.13: Implicit tuple conversions
        // A tuple type can be implicitly converted to another tuple type with the same arity
        // if each element can be implicitly converted.
        if (IsTupleType(sourceType) && IsTupleType(targetType))
        {
            var sourceArgs = sourceType.GetGenericArguments();
            var targetArgs = targetType.GetGenericArguments();

            if (sourceArgs.Length != targetArgs.Length)
                return false;

            return !sourceArgs
                .Where((t, i) => !CanImplicitlyConvert(t, targetArgs[i]))
                .Any();
        }

        // ECMA-334 §10.6.1: Implicit nullable conversions
        // T -> T? (identity lift) and S -> T? (where S -> T is an implicit conversion)
        var underlyingTarget = Nullable.GetUnderlyingType(targetType);
        if (underlyingTarget != null)
        {
            // T -> T?
            if (sourceType == underlyingTarget)
                return true;

            // S -> T? where S -> T is an implicit numeric conversion
            if (ImplicitConversions.TryGetValue(sourceType, out var nullableTargets) && nullableTargets.Contains(underlyingTarget))
                return true;

            // S? -> T? where S -> T is an implicit numeric conversion
            var underlyingSource = Nullable.GetUnderlyingType(sourceType);
            if (underlyingSource != null)
            {
                if (underlyingSource == underlyingTarget)
                    return true;
                if (ImplicitConversions.TryGetValue(underlyingSource, out var liftedTargets) && liftedTargets.Contains(underlyingTarget))
                    return true;
            }
        }

        // Reference type assignability
        if (!targetType.IsValueType && targetType.IsAssignableFrom(sourceType))
            return true;

        // ECMA-334 §10.2.3: Implicit numeric conversions
        if (ImplicitConversions.TryGetValue(sourceType, out var allowedTargets) && allowedTargets.Contains(targetType))
            return true;

        return false;
    }

    /// <summary>
    /// ECMA-334 §12.6.4.7: T1 is a better conversion target than T2 if an implicit conversion
    /// from T1 to T2 exists and no implicit conversion from T2 to T1 exists, or if T1 is a
    /// signed integral type and T2 is an unsigned integral type per the spec's preference table.
    /// Returns positive if T1 is better, negative if T2 is better, zero if neither.
    /// </summary>
    public static int CompareBetterConversionTarget(Type t1, Type t2)
    {
        if (t1 == t2)
            return 0;

        var t1ToT2 = CanImplicitlyConvert(t1, t2) || (!t2.IsValueType && t2.IsAssignableFrom(t1));
        var t2ToT1 = CanImplicitlyConvert(t2, t1) || (!t1.IsValueType && t1.IsAssignableFrom(t2));

        if (t1ToT2 && !t2ToT1) return 1;
        if (t2ToT1 && !t1ToT2) return -1;

        // §12.6.4.7 rule 4: signed integral preferred over unsigned
        var s1 = Nullable.GetUnderlyingType(t1) ?? t1;
        var s2 = Nullable.GetUnderlyingType(t2) ?? t2;
        if (IsSignedPreferredOver(s1, s2)) return 1;
        if (IsSignedPreferredOver(s2, s1)) return -1;

        return 0;
    }

    private static bool IsSignedPreferredOver(Type signed, Type unsigned) =>
        (signed == typeof(sbyte) && unsigned is { } u && (u == typeof(byte) || u == typeof(ushort) || u == typeof(uint) || u == typeof(ulong))) ||
        (signed == typeof(short) && unsigned is { } u2 && (u2 == typeof(ushort) || u2 == typeof(uint) || u2 == typeof(ulong))) ||
        (signed == typeof(int) && unsigned is { } u3 && (u3 == typeof(uint) || u3 == typeof(ulong))) ||
        (signed == typeof(long) && unsigned == typeof(ulong));

    /// <summary>
    /// Checks whether a source type can be assigned or implicitly converted to a target type.
    /// This is the canonical compatibility check used by compile-time emitters when selecting
    /// method overloads and constructors.
    /// </summary>
    public static bool CanAssignOrImplicitlyConvert(Type sourceType, Type targetType)
    {
        if (targetType.IsAssignableFrom(sourceType))
            return true;

        var sourceUnderlying = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
        var targetUnderlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (targetUnderlying.IsAssignableFrom(sourceUnderlying))
            return true;

        return CanImplicitlyConvert(sourceType, targetType) ||
               CanImplicitlyConvert(sourceUnderlying, targetUnderlying);
    }

    /// <summary>
    /// Returns the ECMA-334 binary numeric promotion type for arithmetic operands,
    /// or null when either operand is non-arithmetic.
    /// </summary>
    public static Type? TryGetBinaryNumericPromotionType(Type leftType, Type rightType)
    {
        if (!IsArithmetic(leftType) || !IsArithmetic(rightType))
            return null;

        return NumericDispatch.GetResultType(leftType, rightType);
    }

    /// <summary>
    /// Validates assignment and returns the coerced value, or throws if not implicitly convertible.
    /// Assignment requires implicit convertibility.
    /// </summary>
    public static object? ValidateAssignment(Type targetType, object? value, string varName)
    {
        if (value == null)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                throw new CsEvalException(DiagnosticDescriptors.NullToNonNullable, targetType.Name);
            return null;
        }

        var sourceType = value.GetType();

        // Exact match
        if (sourceType == targetType)
            return value;

        // Nullable type conversion (e.g., int to int?)
        var underlyingTarget = Nullable.GetUnderlyingType(targetType);
        if (underlyingTarget != null)
        {
            if (sourceType == underlyingTarget)
                return value;
            if (ImplicitConversions.TryGetValue(sourceType, out var nullableTargets) && nullableTargets.Contains(underlyingTarget))
                return ConvertNumeric(value, sourceType, underlyingTarget);
        }

        // Reference type assignability (e.g., derived class to base class, array covariance)
        if (!targetType.IsValueType && targetType.IsAssignableFrom(sourceType))
            return value;

        // Allow assigning any T[] to object?[] variables (array covariance for collection expressions)
        if (targetType == typeof(object?[]) && sourceType.IsArray)
            return value;

        // Implicit numeric conversions (widening)
        if (ImplicitConversions.TryGetValue(sourceType, out var allowedTargets) && allowedTargets.Contains(targetType))
            return ConvertNumeric(value, sourceType, targetType);

        // ECMA-334 §10.2.11: implicit constant expression conversion for int literals.
        if (sourceType == typeof(int) && value is int intValue && IsConstantIntConversionTarget(targetType))
        {
            try { return Convert.ChangeType(intValue, targetType); }
            catch (OverflowException)
            {
                throw new CsEvalException(
                    DiagnosticDescriptors.ConstantValueCannotConvert,
                    intValue,
                    targetType.Name);
            }
        }

        // Not implicitly convertible
        throw CreateImplicitConversionException(sourceType, targetType, value);
    }

    private static CsEvalException CreateImplicitConversionException(Type sourceType, Type targetType, object? value)
    {
        var underlyingTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (sourceType == typeof(int) &&
            value is int intValue &&
            IsConstantIntConversionTarget(underlyingTarget))
        {
            try
            {
                Convert.ChangeType(intValue, underlyingTarget);
            }
            catch (OverflowException)
            {
                return new CsEvalException(
                    DiagnosticDescriptors.ConstantValueCannotConvert,
                    intValue,
                    underlyingTarget.Name);
            }
        }

        if (IsNumericOrCharType(sourceType) && IsNumericOrCharType(underlyingTarget))
        {
            return new CsEvalException(
                DiagnosticDescriptors.ExplicitConversionExists,
                sourceType.Name,
                targetType.Name);
        }

        return new CsEvalException(
            DiagnosticDescriptors.NoImplicitConversion,
            sourceType.Name,
            targetType.Name);
    }

    internal static bool IsForbiddenReflectionType(Type? type)
    {
        if (type == null) return false;

        if (typeof(Type).IsAssignableFrom(type))
            return true;

        if (typeof(MemberInfo).IsAssignableFrom(type))
            return true;

        if (typeof(Assembly).IsAssignableFrom(type))
            return true;
        if (typeof(Module).IsAssignableFrom(type))
            return true;

        if (type == typeof(RuntimeTypeHandle) ||
            type == typeof(RuntimeMethodHandle) ||
            type == typeof(RuntimeFieldHandle))
            return true;

        if (typeof(MethodBody).IsAssignableFrom(type))
            return true;

        if (type.Namespace is "System.Reflection.Emit")
            return true;

        if (type.IsPointer || type == typeof(IntPtr) || type == typeof(UIntPtr))
            return true;

        if (type.IsArray && IsForbiddenReflectionType(type.GetElementType()))
            return true;

        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
            {
                if (IsForbiddenReflectionType(arg))
                    return true;
            }
        }

        return false;
    }

    public static object? GuardReflectionLeak(object? value, string context)
    {
        if (value == null) return null;

        var type = value.GetType();
        if (IsForbiddenReflectionType(type))
            throw new CsEvalSandboxException(DiagnosticDescriptors.ReflectionTypeAccessBlocked, type.Name, context);

        return value;
    }

    public static T GuardReflectionLeakTyped<T>(T value, string context)
    {
        // Reflection types are reference types; value types are always safe here.
        if (!typeof(T).IsValueType && value is not null)
        {
            var type = value.GetType();
            if (IsForbiddenReflectionType(type))
                throw new CsEvalSandboxException(DiagnosticDescriptors.ReflectionTypeAccessBlocked, type.Name, context);
        }

        return value;
    }

    internal static bool RequiresReflectionLeakGuard(Type type)
    {
        if (type.IsValueType)
            return false;

        if (type == typeof(string))
            return false;

        if (type == typeof(object))
            return true;

        if (IsForbiddenReflectionType(type))
            return true;

        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            return elementType == null || RequiresReflectionLeakGuard(elementType);
        }

        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
            {
                if (RequiresReflectionLeakGuard(arg))
                    return true;
            }
        }

        // For non-sealed reference types, runtime values can still be forbidden subtypes.
        return !type.IsSealed;
    }

    internal static object? CoerceNumeric(object? arg, Type targetType)
    {
        if (arg == null) return null;
        if (targetType.IsInstanceOfType(arg)) return arg;

        if (arg is IConvertible)
        {
            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
            try
            {
                return Convert.ChangeType(arg, underlying);
            }
            catch (Exception ex) when (ex is InvalidCastException or OverflowException or FormatException)
            {
                throw new CsEvalException(
                    DiagnosticDescriptors.NoImplicitConversion,
                    arg.GetType().Name,
                    targetType.Name);
            }
        }

        throw new CsEvalException(
            DiagnosticDescriptors.NoImplicitConversion,
            arg.GetType().Name,
            targetType.Name);
    }
}
