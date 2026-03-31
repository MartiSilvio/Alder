using Alder.Aot;

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
        if (config.TryGetDispatch(type, out var dispatch) &&
            dispatch.TryInvoke(name, instance, args, out result))
            return true;
        result = null;
        return false;
    }

    internal static bool TryInvokeStatic(
        AlderConfig config, Type type, string name,
        object?[] args, out object? result)
    {
        if (config.TryGetDispatch(type, out var dispatch) &&
            dispatch.TryInvokeStatic(name, args, out result))
            return true;
        result = null;
        return false;
    }

    internal static bool TryGetMember(
        AlderConfig config, Type type, string name,
        object instance, out object? value)
    {
        if (config.TryGetDispatch(type, out var dispatch) &&
            dispatch.TryGet(name, instance, out value))
        {
            value = TypeHelpers.GuardReflectionLeak(value, "member", name);
            return true;
        }
        value = null;
        return false;
    }

    internal static bool TryGetStaticMember(
        AlderConfig config, Type type, string name,
        out object? value)
    {
        if (config.TryGetDispatch(type, out var dispatch) &&
            dispatch.TryGetStatic(name, out value))
        {
            value = TypeHelpers.GuardReflectionLeak(value, "static member", name);
            return true;
        }
        value = null;
        return false;
    }

    internal static bool TrySetMember(
        AlderConfig config, Type type, string name,
        object instance, object? value)
    {
        return config.TryGetDispatch(type, out var dispatch) &&
               dispatch.TrySet(name, instance, value);
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
}
