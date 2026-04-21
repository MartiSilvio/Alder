using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Alder.Aot;

namespace Alder.Runtime.Introspection;

internal static class RuntimeGenericClosure
{
    private static readonly bool RuntimeDynamicCodeSupported =
#if NET7_0_OR_GREATER
        RuntimeFeature.IsDynamicCodeSupported;
#else
        true;
#endif

    private static bool? s_dynamicCodeSupportedOverride;
    private static Type[]? s_builtInClosedGenericTypes;
    private static Type[]? s_builtInClosedDelegateTypes;

    internal static bool DynamicCodeSupported => s_dynamicCodeSupportedOverride ?? RuntimeDynamicCodeSupported;

    internal static IDisposable OverrideDynamicCodeSupportForTesting(bool supported)
    {
        var previous = s_dynamicCodeSupportedOverride;
        s_dynamicCodeSupportedOverride = supported;
        return new RestoreDynamicCodeSupportScope(previous);
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public static Type CloseType(Type openGenericType, Type[] typeArguments)
    {
        return CloseType(openGenericType, typeArguments, rootedDelegateTypes: null);
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public static Type CloseType(Type openGenericType, Type[] typeArguments, IReadOnlyCollection<Type>? rootedDelegateTypes)
    {
        if (TryCloseType(openGenericType, typeArguments, rootedDelegateTypes, out var closedType))
            return closedType!;

        throw new InvalidOperationException(
            $"No rooted generic closure is available for '{openGenericType}'.");
    }

    public static MethodInfo CloseMethod(MethodInfo genericMethod, Type[] typeArguments)
    {
        if (TryCloseMethod(genericMethod, typeArguments, out var closedMethod))
            return closedMethod!;

        throw new InvalidOperationException(
            $"No rooted generic method closure is available for '{genericMethod}'.");
    }

    public static bool TryCloseType(
        Type openGenericType,
        Type[] typeArguments,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] out Type? closedType)
    {
        return TryCloseType(openGenericType, typeArguments, rootedDelegateTypes: null, out closedType);
    }

    public static bool TryCloseType(
        Type openGenericType,
        Type[] typeArguments,
        IReadOnlyCollection<Type>? rootedDelegateTypes,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] out Type? closedType)
    {
        if (TryCloseKnownType(openGenericType, typeArguments, rootedDelegateTypes, out closedType))
            return true;

        if (!DynamicCodeSupported)
        {
            closedType = null;
            return false;
        }

        try
        {
            #pragma warning disable IL3050
            closedType = openGenericType.MakeGenericType(typeArguments);
            #pragma warning restore IL3050
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or TypeLoadException or InvalidOperationException)
        {
            closedType = null;
            return false;
        }
    }

    public static bool TryCloseMethod(MethodInfo genericMethod, Type[] typeArguments, out MethodInfo? closedMethod)
    {
        if (!DynamicCodeSupported)
        {
            closedMethod = null;
            return false;
        }

        try
        {
            #pragma warning disable IL3050
            closedMethod = genericMethod.MakeGenericMethod(typeArguments);
            #pragma warning restore IL3050
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or TypeLoadException or InvalidOperationException)
        {
            closedMethod = null;
            return false;
        }
    }

    private static bool TryCloseKnownType(
        Type openGenericType,
        Type[] typeArguments,
        IReadOnlyCollection<Type>? rootedDelegateTypes,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] out Type? closedType)
    {
        if (openGenericType == typeof(Nullable<>))
            return TryCloseNullableType(typeArguments, out closedType);

        if (TryCloseDelegateType(openGenericType, typeArguments, rootedDelegateTypes, out closedType))
            return true;

        if (TryCloseBuiltInGenericType(openGenericType, typeArguments, out closedType))
            return true;

        closedType = null;
        return false;
    }

    private static bool TryCloseBuiltInGenericType(
        Type openGenericType,
        Type[] typeArguments,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] out Type? closedType)
    {
        foreach (var candidate in GetBuiltInClosedGenericTypes())
        {
            if (candidate.GetGenericTypeDefinition() != openGenericType)
                continue;

            var candidateArgs = candidate.GetGenericArguments();
            if (candidateArgs.Length != typeArguments.Length)
                continue;

            var matches = !candidateArgs.Where((t, i) => t != typeArguments[i]).Any();

            if (matches)
            {
                closedType = candidate;
                return true;
            }
        }

        closedType = null;
        return false;
    }

    private static bool TryCloseNullableType(
        Type[] typeArguments,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] out Type? closedType)
    {
        if (typeArguments.Length != 1)
        {
            closedType = null;
            return false;
        }

        closedType = typeArguments[0] switch
        {
            var t when t == typeof(bool) => typeof(bool?),
            var t when t == typeof(byte) => typeof(byte?),
            var t when t == typeof(sbyte) => typeof(sbyte?),
            var t when t == typeof(short) => typeof(short?),
            var t when t == typeof(ushort) => typeof(ushort?),
            var t when t == typeof(int) => typeof(int?),
            var t when t == typeof(uint) => typeof(uint?),
            var t when t == typeof(long) => typeof(long?),
            var t when t == typeof(ulong) => typeof(ulong?),
            var t when t == typeof(char) => typeof(char?),
            var t when t == typeof(float) => typeof(float?),
            var t when t == typeof(double) => typeof(double?),
            var t when t == typeof(decimal) => typeof(decimal?),
            var t when t == typeof(DateTime) => typeof(DateTime?),
            var t when t == typeof(TimeSpan) => typeof(TimeSpan?),
            var t when t == typeof(Guid) => typeof(Guid?),
            _ => null
        };

        return closedType != null;
    }

    private static bool TryCloseDelegateType(
        Type openGenericType,
        Type[] typeArguments,
        IReadOnlyCollection<Type>? rootedDelegateTypes,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] out Type? closedType)
    {
        if (!typeof(Delegate).IsAssignableFrom(openGenericType))
        {
            closedType = null;
            return false;
        }

        if (TryFindClosedGenericType(GetBuiltInClosedDelegateTypes(), openGenericType, typeArguments, out closedType))
            return true;

        if (rootedDelegateTypes != null &&
            TryFindClosedGenericType(rootedDelegateTypes, openGenericType, typeArguments, out closedType))
        {
            return true;
        }

        closedType = null;
        return false;
    }

    private static Type[] GetBuiltInClosedGenericTypes()
    {
        return s_builtInClosedGenericTypes ??= AlderBuiltInContext.Default
            .GetTypeMetadata()
            .Select(static metadata => metadata.Type)
            .Where(static type => type.IsGenericType && !type.ContainsGenericParameters)
            .Distinct()
            .ToArray();
    }

    private static Type[] GetBuiltInClosedDelegateTypes()
    {
        return s_builtInClosedDelegateTypes ??= AlderBuiltInContext.Default
            .GetClosedDelegateTypes()?
            .Distinct()
            .ToArray() ?? [];
    }

    private static bool TryFindClosedGenericType(
        IEnumerable<Type> candidates,
        Type openGenericType,
        Type[] typeArguments,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] out Type? closedType)
    {
        foreach (var candidate in candidates)
        {
            if (!candidate.IsGenericType || candidate.ContainsGenericParameters)
                continue;

            if (candidate.GetGenericTypeDefinition() != openGenericType)
                continue;

            var candidateArgs = candidate.GetGenericArguments();
            if (candidateArgs.Length != typeArguments.Length)
                continue;

            var matches = true;
            for (var i = 0; i < candidateArgs.Length; i++)
            {
                if (candidateArgs[i] != typeArguments[i])
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                closedType = candidate;
                return true;
            }
        }

        closedType = null;
        return false;
    }

    private sealed class RestoreDynamicCodeSupportScope(bool? previous) : IDisposable
    {
        public void Dispose()
        {
            s_dynamicCodeSupportedOverride = previous;
        }
    }
}
