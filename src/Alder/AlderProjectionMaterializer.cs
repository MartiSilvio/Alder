using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder;

/// <summary>
/// Materializes Alder structural projections into user-supplied CLR DTO or record types.
/// </summary>
internal static class AlderProjectionMaterializer
{
    private readonly record struct ConstructorCandidate(
        ConstructorInfo Constructor,
        ParameterInfo[] Parameters,
        int MatchedParameterCount,
        int DefaultedParameterCount,
        int ExactMatchCount);

    [RequiresUnreferencedCode("Projection materialization inspects public constructors and public properties of the target type.")]
    internal static T? Materialize<T>(object? value)
    {
        if (TryMaterializeCore(value, typeof(T), out var materialized, out var failureReason))
            return (T?)materialized;

        var sourceName = value?.GetType().Name ?? "null";
        throw new AlderException(
            DiagnosticDescriptors.ProjectionMaterializationFailed,
            sourceName,
            typeof(T).Name,
            failureReason ?? "No compatible public constructor or writable property mapping was found.");
    }

    [RequiresUnreferencedCode("Projection materialization inspects public constructors and public properties of the target type.")]
    internal static bool TryMaterialize<
        T>(object? value,
        out T? result)
    {
        if (!TryMaterializeCore(value, typeof(T), out var materialized, out _))
        {
            result = default;
            return false;
        }

        result = (T?)materialized;
        return true;
    }

    [RequiresUnreferencedCode("Projection materialization inspects public constructors and public properties of the target type.")]
    internal static T? Materialize<T>(StructuralObjectValue value)
    {
        if (TryMaterializeCore(value, typeof(T), out var materialized, out var failureReason))
            return (T?)materialized;

        throw new AlderException(
            DiagnosticDescriptors.ProjectionMaterializationFailed,
            value.GetType().Name,
            typeof(T).Name,
            failureReason ?? "No compatible public constructor or writable property mapping was found.");
    }

    internal static bool IsSupportedTargetType(Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (underlying == typeof(object) ||
            underlying == typeof(string) ||
            underlying.IsPrimitive ||
            underlying.IsEnum ||
            underlying == typeof(decimal) ||
            underlying == typeof(DateTime) ||
            underlying == typeof(DateTimeOffset) ||
            underlying == typeof(TimeSpan) ||
            underlying == typeof(Guid))
        {
            return false;
        }

        if (typeof(Delegate).IsAssignableFrom(underlying) ||
            typeof(System.Collections.IEnumerable).IsAssignableFrom(underlying) && underlying != typeof(byte[]))
        {
            return false;
        }

        return underlying is { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false };
    }

#pragma warning disable IL2067, IL2070
    internal static bool TryMaterializeCore(
        object? value,
        Type targetType,
        out object? result,
        out string? failureReason)
    {
        if (value == null)
        {
            result = targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null
                ? Activator.CreateInstance(targetType)
                : null;
            failureReason = null;
            return true;
        }

        if (targetType.IsInstanceOfType(value))
        {
            result = value;
            failureReason = null;
            return true;
        }

        if (value is not StructuralObjectValue projection)
        {
            result = null;
            failureReason = "Source value is not an Alder structural projection.";
            return false;
        }

        if (!IsSupportedTargetType(targetType))
        {
            result = null;
            failureReason = $"Target type '{targetType.Name}' is not a supported DTO or record target.";
            return false;
        }

        try
        {
            return TryCreateTarget(targetType, projection, out result, out failureReason);
        }
        catch (AlderException ex)
        {
            result = null;
            failureReason = ex.Message;
            return false;
        }
    }
#pragma warning restore IL2067, IL2070

#pragma warning disable IL2067, IL2070
    private static bool TryCreateTarget(
        Type targetType,
        StructuralObjectValue members,
        out object? result,
        out string? failureReason)
    {
        var constructors = targetType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var constructor = SelectConstructor(constructors, members);
        HashSet<string>? usedMembers = null;
        var anyBound = false;

        if (constructor != null)
        {
            var args = BindConstructorArguments(constructor.Value.Parameters, members, ref usedMembers, ref anyBound);
            result = constructor.Value.Constructor.Invoke(args);
        }
        else
        {
            ConstructorInfo? parameterless = null;
            foreach (var candidate in constructors)
            {
                if (candidate.GetParameters().Length == 0)
                {
                    parameterless = candidate;
                    break;
                }
            }

            if (parameterless == null)
            {
                result = null;
                failureReason = "No satisfiable public constructor was found.";
                return false;
            }

            result = parameterless.Invoke([]);
        }

        BindWritableProperties(result!, properties, members, usedMembers, ref anyBound);
        if (!anyBound)
        {
            result = null;
            failureReason = "Projection members did not match any constructor parameter or writable property.";
            return false;
        }

        failureReason = null;
        return true;
    }
#pragma warning restore IL2067, IL2070

    private static ConstructorCandidate? SelectConstructor(
        ConstructorInfo[] constructors,
        StructuralObjectValue members)
    {
        var best = default(ConstructorCandidate?);
        var ambiguous = false;

        foreach (var constructor in constructors)
        {
            if (!TryCreateConstructorCandidate(constructor, members, out var candidate))
                continue;

            if (best == null)
            {
                best = candidate;
                ambiguous = false;
                continue;
            }

            var comparison = CompareCandidates(candidate, best.Value);
            if (comparison > 0)
            {
                best = candidate;
                ambiguous = false;
            }
            else if (comparison == 0)
            {
                ambiguous = true;
            }
        }

        if (ambiguous && best is { MatchedParameterCount: > 0 } resolved)
            throw new AlderException(
                DiagnosticDescriptors.ProjectionMaterializationFailed,
                "projection",
                resolved.Constructor.DeclaringType!.Name,
                "Multiple public constructors match the projection members.");

        return best;
    }

    private static bool TryCreateConstructorCandidate(
        ConstructorInfo constructor,
        StructuralObjectValue members,
        out ConstructorCandidate candidate)
    {
        var parameters = constructor.GetParameters();
        var matched = 0;
        var defaulted = 0;
        var exact = 0;

        foreach (var parameter in parameters)
        {
            if (TryGetProjectionMember(members, parameter.Name!, out var value))
            {
                if (!CanBindMemberValue(value, parameter.ParameterType))
                {
                    candidate = default;
                    return false;
                }

                matched++;
                if (IsExactMemberMatch(value, parameter.ParameterType))
                    exact++;
                continue;
            }

            if (!parameter.HasDefaultValue)
            {
                candidate = default;
                return false;
            }

            defaulted++;
        }

        candidate = new ConstructorCandidate(constructor, parameters, matched, defaulted, exact);
        return true;
    }

    private static int CompareCandidates(ConstructorCandidate left, ConstructorCandidate right)
    {
        var matched = left.MatchedParameterCount.CompareTo(right.MatchedParameterCount);
        if (matched != 0)
            return matched;

        var defaulted = right.DefaultedParameterCount.CompareTo(left.DefaultedParameterCount);
        if (defaulted != 0)
            return defaulted;

        return left.ExactMatchCount.CompareTo(right.ExactMatchCount);
    }

    private static object?[] BindConstructorArguments(
        ParameterInfo[] parameters,
        StructuralObjectValue members,
        ref HashSet<string>? usedMembers,
        ref bool anyBound)
    {
        var args = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (TryGetProjectionMember(members, parameter.Name!, out var value))
            {
                args[i] = ConvertMemberValue(value, parameter.ParameterType, parameter.Name!);
                MarkMemberUsed(ref usedMembers, parameter.Name!);
                anyBound = true;
                continue;
            }

            args[i] = parameter.DefaultValue;
        }

        return args;
    }

    private static void BindWritableProperties(
        object instance,
        PropertyInfo[] properties,
        StructuralObjectValue members,
        HashSet<string>? usedMembers,
        ref bool anyBound)
    {
        foreach (var property in properties)
        {
            if (!property.CanWrite || property.GetIndexParameters().Length != 0)
                continue;
            if (usedMembers?.Contains(property.Name) == true)
                continue;
            if (!TryGetProjectionMember(members, property.Name, out var value))
                continue;

            property.SetValue(instance, ConvertMemberValue(value, property.PropertyType, property.Name));
            anyBound = true;
        }
    }

    private static bool CanBindMemberValue(object? value, Type targetType)
    {
        if (value == null)
            return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;

        if (targetType.IsInstanceOfType(value))
            return true;

        if (value is StructuralObjectValue projection)
            return TryMaterializeCore(projection, targetType, out _, out _);

        var underlyingTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var sourceType = value.GetType();

        if (targetType.IsAssignableFrom(sourceType) ||
            underlyingTarget.IsAssignableFrom(sourceType) ||
            TypeHelpers.CanImplicitlyConvert(sourceType, targetType) ||
            TypeHelpers.CanImplicitlyConvert(sourceType, underlyingTarget) ||
            underlyingTarget.IsEnum && TypeHelpers.IsInteger(value) ||
            underlyingTarget == typeof(char) && value is string { Length: 1 })
        {
            return true;
        }

        try
        {
            _ = ConvertMemberValue(value, targetType, "__projection__");
            return true;
        }
        catch (AlderException)
        {
            return false;
        }
    }

    private static bool IsExactMemberMatch(object? value, Type targetType)
    {
        if (value == null)
            return false;

        var sourceType = value.GetType();
        var underlyingTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return sourceType == targetType || sourceType == underlyingTarget;
    }

    private static object? ConvertMemberValue(
        object? value,
        Type targetType,
        string memberName)
    {
        if (value == null)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                throw new AlderException(DiagnosticDescriptors.NullToNonNullable, targetType.Name);
            return null;
        }

        if (targetType.IsInstanceOfType(value))
            return value;

        if (value is StructuralObjectValue projection &&
            TryMaterializeCore(projection, targetType, out var nestedResult, out _))
        {
            return nestedResult;
        }

        return TypeHelpers.ValidateAndCoerceType(targetType, value, memberName, isConstantExpression: false);
    }

    private static bool TryGetProjectionMember(StructuralObjectValue members, string name, out object? value) =>
        members.TryGetValue(name, isCaseSensitive: false, out value);

    private static void MarkMemberUsed(ref HashSet<string>? usedMembers, string name)
    {
        usedMembers ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        usedMembers.Add(name);
    }
}
