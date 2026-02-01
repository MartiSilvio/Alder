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

        // §10.2.11: Implicit constant expression conversions
        // An int constant can be assigned to smaller types if value is in range
        if (sourceType == typeof(int) && value is int intValue)
        {
            if (TryConstantConversion(intValue, underlyingType, out var converted))
                return converted;
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
    /// Per §10.2: Assignment requires implicit convertibility.
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

    /// <summary>
    /// §10.2.11: Implicit constant expression conversions.
    /// An int constant can be converted to sbyte, byte, short, ushort, uint, ulong
    /// if the value is within the target type's range.
    /// </summary>
    private static bool TryConstantConversion(int value, Type targetType, out object? result)
    {
        result = null;

        if (targetType == typeof(sbyte) && value >= sbyte.MinValue && value <= sbyte.MaxValue)
        {
            result = (sbyte)value;
            return true;
        }
        if (targetType == typeof(byte) && value >= byte.MinValue && value <= byte.MaxValue)
        {
            result = (byte)value;
            return true;
        }
        if (targetType == typeof(short) && value >= short.MinValue && value <= short.MaxValue)
        {
            result = (short)value;
            return true;
        }
        if (targetType == typeof(ushort) && value >= ushort.MinValue && value <= ushort.MaxValue)
        {
            result = (ushort)value;
            return true;
        }
        if (targetType == typeof(uint) && value >= 0)
        {
            result = (uint)value;
            return true;
        }
        if (targetType == typeof(ulong) && value >= 0)
        {
            result = (ulong)value;
            return true;
        }

        return false;
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
