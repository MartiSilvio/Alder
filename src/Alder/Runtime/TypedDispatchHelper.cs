using System.Reflection;

namespace Alder.Runtime;

/// <summary>
/// Centralized typed dispatch checks. Each method tries the AOT-generated dispatch path
/// and returns false if the type has no registered dispatch or the dispatch doesn't handle
/// the requested operation. Call sites that need reflection fallback do so after this returns false.
/// </summary>
internal static class TypedDispatchHelper
{
    internal static bool TryInvokeInstance(
        AlderConfig config, Type type, string name,
        object instance, object?[] args, out object? result)
    {
        if (config.TryGetDispatch(type, out var dispatch))
        {
            if (dispatch.TryInvoke(name, instance, args, out result))
                return true;

            var canonicalName = ResolveCanonicalName(config, type, name, isStatic: false, MemberTypes.Method);
            if (canonicalName != null && dispatch.TryInvoke(canonicalName, instance, args, out result))
                return true;
        }

        result = null;
        return false;
    }

    internal static bool TryInvokeStatic(
        AlderConfig config, Type type, string name,
        object?[] args, out object? result)
    {
        if (config.TryGetDispatch(type, out var dispatch))
        {
            if (dispatch.TryInvokeStatic(name, args, out result))
                return true;

            var canonicalName = ResolveCanonicalName(config, type, name, isStatic: true, MemberTypes.Method);
            if (canonicalName != null && dispatch.TryInvokeStatic(canonicalName, args, out result))
                return true;
        }

        result = null;
        return false;
    }

    internal static bool TryGetMember(
        AlderConfig config, Type type, string name,
        object instance, out object? value)
    {
        if (config.TryGetDispatch(type, out var dispatch))
        {
            if (dispatch.TryGet(name, instance, out value))
            {
                value = TypeHelpers.GuardReflectionLeak(value, "member", name);
                return true;
            }

            var canonicalName = ResolveCanonicalName(config, type, name, isStatic: false, MemberTypes.Property | MemberTypes.Field);
            if (canonicalName != null && dispatch.TryGet(canonicalName, instance, out value))
            {
                value = TypeHelpers.GuardReflectionLeak(value, "member", name);
                return true;
            }
        }

        value = null;
        return false;
    }

    internal static bool TryGetStaticMember(
        AlderConfig config, Type type, string name,
        out object? value)
    {
        if (config.TryGetDispatch(type, out var dispatch))
        {
            if (dispatch.TryGetStatic(name, out value))
            {
                value = TypeHelpers.GuardReflectionLeak(value, "static member", name);
                return true;
            }

            var canonicalName = ResolveCanonicalName(config, type, name, isStatic: true, MemberTypes.Property | MemberTypes.Field);
            if (canonicalName != null && dispatch.TryGetStatic(canonicalName, out value))
            {
                value = TypeHelpers.GuardReflectionLeak(value, "static member", name);
                return true;
            }
        }

        value = null;
        return false;
    }

    internal static bool TrySetMember(
        AlderConfig config, Type type, string name,
        object instance, object? value)
    {
        if (!config.TryGetDispatch(type, out var dispatch))
            return false;

        if (dispatch.TrySet(name, instance, value))
            return true;

        var canonicalName = ResolveCanonicalName(config, type, name, isStatic: false, MemberTypes.Property | MemberTypes.Field);
        return canonicalName != null && dispatch.TrySet(canonicalName, instance, value);
    }

    internal static bool TryGetIndex(
        AlderConfig config, Type type,
        object instance, object key, out object? value)
    {
        if (config.TryGetDispatch(type, out var dispatch) &&
            dispatch.TryGetIndex(instance, key, out value))
        {
            value = TypeHelpers.GuardReflectionLeak(value, "indexer");
            return true;
        }
        value = null;
        return false;
    }

    internal static bool TrySetIndex(
        AlderConfig config, Type type,
        object instance, object key, object? value)
    {
        return config.TryGetDispatch(type, out var dispatch) &&
               dispatch.TrySetIndex(instance, key, value);
    }

    internal static bool TryCreate(
        AlderConfig config, Type type,
        object?[] args, out object? instance)
    {
        if (config.TypeDispatch is { } dispatch &&
            dispatch.TryGetValue(type, out var metadata) &&
            metadata.TryCreate(args, out instance))
            return true;
        instance = null;
        return false;
    }

    /// <summary>
    /// In case-insensitive mode, resolves the canonical (PascalCase) member name via reflection
    /// so AOT dispatch, which uses exact names, can match. Returns null if case-sensitive
    /// or if no matching member exists. Returns null (not the original name) when the canonical
    /// name equals the input, so callers can skip the retry.
    /// </summary>
    private static string? ResolveCanonicalName(
        AlderConfig config, Type type, string name, bool isStatic, MemberTypes memberTypes)
    {
        if (config.IsCaseSensitive)
            return null;

        var flags = BindingFlags.Public | BindingFlags.IgnoreCase
            | (isStatic ? BindingFlags.Static : BindingFlags.Instance);

        if ((memberTypes & MemberTypes.Property) != 0)
        {
            var property = config.TypeMetadata.GetProperty(type, name, flags);
            if (property != null && property.Name != name)
                return property.Name;
        }

        if ((memberTypes & MemberTypes.Field) != 0)
        {
            var field = config.TypeMetadata.GetField(type, name, flags);
            if (field != null && field.Name != name)
                return field.Name;
        }

        if ((memberTypes & MemberTypes.Method) != 0)
        {
            var methods = config.TypeMetadata.GetMethods(type, name, flags);
            if (methods.Length > 0 && methods[0].Name != name)
                return methods[0].Name;
        }

        return null;
    }
}
