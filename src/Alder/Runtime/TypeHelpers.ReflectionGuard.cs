using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Alder.Diagnostics;

namespace Alder.Runtime;

internal static partial class TypeHelpers
{
    private static readonly ConcurrentDictionary<Type, bool> ForbiddenTypeCache = new();

    internal static bool IsForbiddenReflectionType(Type? type)
    {
        if (type == null) return false;

        return ForbiddenTypeCache.GetOrAdd(type, static t => IsForbiddenReflectionTypeCore(t));
    }

    private static bool IsForbiddenReflectionTypeCore(Type type)
    {
        // Type objects are safe metadata (typeof(), GetType(), GetGenericTypeDefinition()).
        // They can't invoke code or bypass the sandbox on their own.
        // Dangerous operations on Type (GetMethod, GetField, etc.) return MemberInfo
        // subtypes which ARE still blocked below.
        if (typeof(Type).IsAssignableFrom(type))
            return false;

        // Block other MemberInfo subtypes (MethodInfo, PropertyInfo, FieldInfo, etc.)
        // which enable dynamic invocation and bypass the sandbox.
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? GuardReflectionLeak(object? value, string context)
    {
        if (value == null) return null;
        if (IsForbiddenReflectionType(value.GetType()))
            ThrowReflectionLeak(value.GetType(), context);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? GuardReflectionLeak(object? value, string memberKind, string memberName)
    {
        if (value == null) return null;
        if (IsForbiddenReflectionType(value.GetType()))
            ThrowReflectionLeak(value.GetType(), memberKind, memberName);
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowReflectionLeak(Type type, string context) =>
        throw new AlderException(DiagnosticDescriptors.ReflectionTypeAccessBlocked, type.Name, context);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowReflectionLeak(Type type, string memberKind, string memberName) =>
        throw new AlderException(DiagnosticDescriptors.ReflectionTypeAccessBlocked, type.Name, $"{memberKind} {memberName}");

    public static T GuardReflectionLeakTyped<T>(T value, string context)
    {
        if (!typeof(T).IsValueType && value is not null)
        {
            var type = value.GetType();
            if (IsForbiddenReflectionType(type))
                throw new AlderException(DiagnosticDescriptors.ReflectionTypeAccessBlocked, type.Name, context);
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
}
