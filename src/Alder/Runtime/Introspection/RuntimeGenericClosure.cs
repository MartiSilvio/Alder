using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Alder.Aot;
using Alder.Diagnostics;

namespace Alder.Runtime.Introspection;

internal static class RuntimeGenericClosure
{
    private static bool? s_dynamicCodeSupportedOverride;
    private static RootedType[]? s_builtInRootedGenericTypes;
    private static RootedType[]? s_builtInRootedDelegateTypes;

    // Single source of truth for whether runtime-emitted generic closures (MakeGenericType /
    // MakeGenericMethod) are usable. A test override, when set, is authoritative — so a harness can
    // force either path on, including under NativeAOT publish where the runtime flag is false.
    // With no override it reports the real runtime capability.
    internal static bool DynamicCodeSupported =>
#if NET7_0_OR_GREATER
        s_dynamicCodeSupportedOverride ?? RuntimeFeature.IsDynamicCodeSupported;
#else
        s_dynamicCodeSupportedOverride ?? true;
#endif

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
    public static Type CloseType(Type openGenericType, Type[] typeArguments, IReadOnlyCollection<RootedType>? rootedDelegateTypes)
    {
        if (TryCloseType(openGenericType, typeArguments, rootedDelegateTypes, out var closedType))
            return closedType!;

        throw new AlderException(DiagnosticDescriptors.GeneratedClosureRequired, openGenericType);
    }

    public static MethodInfo CloseMethod(MethodInfo genericMethod, Type[] typeArguments)
    {
        if (TryCloseMethod(genericMethod, typeArguments, out var closedMethod))
            return closedMethod!;

        throw new AlderException(DiagnosticDescriptors.GeneratedClosureRequired, genericMethod);
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
        IReadOnlyCollection<RootedType>? rootedDelegateTypes,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] out Type? closedType)
    {
        if (TryCloseKnownType(openGenericType, typeArguments, rootedDelegateTypes, out closedType))
            return true;

        if (!DynamicCodeSupported)
        {
            closedType = null;
            return false;
        }

#if NET7_0_OR_GREATER
        // Direct RuntimeFeature guard: the ILLink/AOT analyzer recognizes this intrinsic and proves
        // the MakeGenericType call below unreachable under NativeAOT. DynamicCodeSupported wraps it
        // behind a property/override the analyzer cannot see, so this guard must stay to keep the
        // AOT publish free of IL2055/IL3050 warnings.
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            closedType = null;
            return false;
        }
#endif

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

#if NET7_0_OR_GREATER
        // See TryCloseType: the analyzer-recognized RuntimeFeature guard keeps the MakeGenericMethod
        // call below provably unreachable under NativeAOT (suppresses IL2060/IL3050).
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            closedMethod = null;
            return false;
        }
#endif

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
        IReadOnlyCollection<RootedType>? rootedDelegateTypes,
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
        foreach (var rootedCandidate in GetBuiltInRootedGenericTypes())
        {
            var candidate = rootedCandidate.Type;
            if (candidate.GetGenericTypeDefinition() != openGenericType)
                continue;

            var candidateArgs = candidate.GetGenericArguments();
            if (candidateArgs.Length != typeArguments.Length)
                continue;

            var matches = !candidateArgs.Where((t, i) => t != typeArguments[i]).Any();

            if (matches)
            {
                closedType = rootedCandidate.Type;
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
        IReadOnlyCollection<RootedType>? rootedDelegateTypes,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] out Type? closedType)
    {
        if (!typeof(Delegate).IsAssignableFrom(openGenericType))
        {
            closedType = null;
            return false;
        }

        if (TryFindClosedGenericType(GetBuiltInRootedDelegateTypes(), openGenericType, typeArguments, out closedType))
            return true;

        if (rootedDelegateTypes != null &&
            TryFindClosedGenericType(rootedDelegateTypes, openGenericType, typeArguments, out closedType))
        {
            return true;
        }

        closedType = null;
        return false;
    }

    private static RootedType[] GetBuiltInRootedGenericTypes()
    {
        return s_builtInRootedGenericTypes ??= AlderBuiltInContext.Default
            .GetTypeMetadata()
            .Select(static metadata => new RootedType(metadata.Type))
            .Where(static rootedType => rootedType.Type.IsGenericType && !rootedType.Type.ContainsGenericParameters)
            .Distinct()
            .ToArray();
    }

    private static RootedType[] GetBuiltInRootedDelegateTypes()
    {
        return s_builtInRootedDelegateTypes ??= AlderBuiltInContext.Default
            .GetRootedDelegateTypes()?
            .Distinct()
            .ToArray() ?? [];
    }

    private static bool TryFindClosedGenericType(
        IEnumerable<RootedType> candidates,
        Type openGenericType,
        Type[] typeArguments,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] out Type? closedType)
    {
        foreach (var rootedCandidate in candidates)
        {
            var candidate = rootedCandidate.Type;
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
                closedType = rootedCandidate.Type;
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
