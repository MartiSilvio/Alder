using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace CsEval.Runtime;

/// <summary>
/// Type checking, validation, and conversion utilities.
/// </summary>
public static class TypeHelpers
{
    public static bool RequireBoolean(object? value)
    {
        if (value is bool b)
            return b;

        throw new CsEvalException($"Condition must evaluate to a boolean, got '{value?.GetType().Name ?? "null"}'");
    }

    internal static bool IsInteger(object? value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong;

    internal static bool IsNumeric(object? value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;

    private static bool IsIntegerType(Type type) =>
        Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.UInt64;

    private static readonly Dictionary<string, Type> TypeNameToClrType = new()
    {
        ["sbyte"] = typeof(sbyte),
        ["byte"] = typeof(byte),
        ["short"] = typeof(short),
        ["ushort"] = typeof(ushort),
        ["int"] = typeof(int),
        ["uint"] = typeof(uint),
        ["long"] = typeof(long),
        ["ulong"] = typeof(ulong),
        ["float"] = typeof(float),
        ["double"] = typeof(double),
        ["decimal"] = typeof(decimal),
        ["bool"] = typeof(bool),
        ["char"] = typeof(char),
        ["string"] = typeof(string),
        ["object"] = typeof(object),
        ["sbyte?"] = typeof(sbyte?),
        ["byte?"] = typeof(byte?),
        ["short?"] = typeof(short?),
        ["ushort?"] = typeof(ushort?),
        ["int?"] = typeof(int?),
        ["uint?"] = typeof(uint?),
        ["long?"] = typeof(long?),
        ["ulong?"] = typeof(ulong?),
        ["float?"] = typeof(float?),
        ["double?"] = typeof(double?),
        ["decimal?"] = typeof(decimal?),
        ["bool?"] = typeof(bool?),
        ["char?"] = typeof(char?),
        ["string?"] = typeof(string),
        ["object?"] = typeof(object),
    };

    private static readonly Dictionary<Type, HashSet<Type>> ImplicitConversions = new()
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
    };

    public static Type ResolveTypeName(string typeName)
    {
        if (TypeNameToClrType.TryGetValue(typeName, out var type))
            return type;
        throw new CsEvalException($"Unknown type '{typeName}'");
    }

    /// <summary>
    /// Performs an explicit cast with optional static type checking.
    /// When sourceStaticType is 'object', enforces C# unboxing semantics:
    /// you can only unbox to the exact boxed type.
    /// </summary>
    public static object? ExplicitCast(object? value, string targetTypeName, Type? sourceStaticType = null)
    {
        if (!TypeNameToClrType.TryGetValue(targetTypeName, out var targetType))
            throw new CsEvalException($"Unknown type '{targetTypeName}'");

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var isNullable = Nullable.GetUnderlyingType(targetType) != null;

        if (value == null)
        {
            if (targetType.IsValueType && !isNullable)
                throw new CsEvalException($"Cannot cast null to non-nullable type '{targetTypeName}'");
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
            throw new InvalidCastException($"Cannot cast {runtimeType.Name} to string");
        }

        if (underlyingType == typeof(object))
            return value;

        // C# unboxing rule: when source static type is 'object', you can only unbox to the exact boxed type
        // (long)(object)42 fails because 42 is boxed as int, not long
        if (sourceStaticType == typeof(object) && underlyingType.IsValueType && runtimeType != underlyingType)
        {
            throw new InvalidCastException($"Unable to cast object of type '{runtimeType.Name}' to type '{underlyingType.Name}'.");
        }

        // Numeric and char conversions
        try
        {
            if (underlyingType == typeof(char) && value is string { Length: 1 } s)
                return s[0];

            return RuntimeCast(value, runtimeType, underlyingType);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            throw new InvalidCastException($"Cannot cast {runtimeType.Name} to {targetTypeName}", ex);
        }
    }

    private static readonly ConcurrentDictionary<(Type, Type), Func<object, object>> CastCache = new();

    private static object RuntimeCast(object value, Type sourceType, Type targetType)
    {
        var converter = CastCache.GetOrAdd((sourceType, targetType), key =>
        {
            var param = LinqExpression.Parameter(typeof(object), "value");
            var unbox = LinqExpression.Convert(param, key.Item1);
            var cast = LinqExpression.Convert(unbox, key.Item2);
            var box = LinqExpression.Convert(cast, typeof(object));
            return LinqExpression.Lambda<Func<object, object>>(box, param).Compile();
        });
        return converter(value);
    }

    public static bool IsType(object? value, string typeName)
    {
        if (!TypeNameToClrType.TryGetValue(typeName, out var targetType))
            throw new CsEvalException($"Unknown type '{typeName}'");

        if (value == null)
            return false;

        var valueType = value.GetType();
        var underlyingTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return underlyingTarget.IsAssignableFrom(valueType) || valueType == underlyingTarget;
    }

    public static object? TryAs(object? value, string typeName)
    {
        if (!TypeNameToClrType.TryGetValue(typeName, out var targetType))
            throw new CsEvalException($"Unknown type '{typeName}'");

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

    public static object? ValidateAndCoerceType(string typeName, object? value, string varName)
    {
        if (typeName == "object")
            return value;

        if (!TypeNameToClrType.TryGetValue(typeName, out var targetType))
            throw new CsEvalException($"Unknown type '{typeName}'");

        var isNullable = Nullable.GetUnderlyingType(targetType) != null;
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value == null)
        {
            if (targetType.IsValueType && !isNullable)
                throw new CsEvalException($"Cannot assign null to {typeName} variable '{varName}'");
            return null;
        }

        var sourceType = value.GetType();

        if (sourceType == underlyingType || sourceType == targetType)
            return value;

        if (ImplicitConversions.TryGetValue(sourceType, out var allowedTargets) && allowedTargets.Contains(underlyingType))
            return Convert.ChangeType(value, underlyingType);

        // Implicit constant expression conversions - only for narrowing to smaller integer types
        if (sourceType == typeof(int) && value is int intValue && IsIntegerType(underlyingType))
        {
            try { return Convert.ChangeType(intValue, underlyingType); }
            catch (OverflowException) { }
        }

        if (underlyingType == typeof(char) && value is string { Length: 1 } s)
            return s[0];

        throw new CsEvalException($"Cannot assign {sourceType.Name} to {typeName} variable '{varName}'");
    }

    /// <summary>
    /// Checks if a value can be implicitly assigned to a target type per C# rules.
    /// Used for assignment validation.
    /// </summary>
    public static bool CanImplicitlyConvert(Type sourceType, Type targetType)
    {
        if (sourceType == targetType)
            return true;

        // Reference type assignability
        if (!targetType.IsValueType && targetType.IsAssignableFrom(sourceType))
            return true;

        if (ImplicitConversions.TryGetValue(sourceType, out var allowedTargets) && allowedTargets.Contains(targetType))
            return true;

        return false;
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
                throw new CsEvalException($"Cannot assign null to non-nullable type '{targetType.Name}'");
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
                return Convert.ChangeType(value, underlyingTarget);
        }

        // Reference type assignability (e.g., derived class to base class, array covariance)
        if (!targetType.IsValueType && targetType.IsAssignableFrom(sourceType))
            return value;

        // Allow assigning any List<T> to List<object?> variables (common pattern: var x = []; x = [...x, item])
        if (targetType == typeof(List<object?>) && sourceType.IsGenericType &&
            sourceType.GetGenericTypeDefinition() == typeof(List<>))
            return value;

        // Implicit numeric conversions (widening)
        if (ImplicitConversions.TryGetValue(sourceType, out var allowedTargets) && allowedTargets.Contains(targetType))
            return Convert.ChangeType(value, targetType);

        // Not implicitly convertible
        throw new CsEvalException($"Cannot implicitly convert type '{sourceType.Name}' to '{targetType.Name}'");
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

    public static object? CheckSandboxType(object? value, SandboxOptions options)
    {
        if (value == null) return null;

        var type = value.GetType();
        if (IsForbiddenReflectionType(type))
        {
            throw new CsEvalException($"Access to reflection types is not allowed: {type.Name}");
        }

        return value;
    }
}
