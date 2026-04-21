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
        object? resolved = null;
        var success = TryDispatchNamed(
            config, type, name, isStatic: false, MemberTypes.Method,
            dispatch => dispatch.TryInvoke(name, instance, args, out resolved),
            dispatchName => dispatchName.Dispatch.TryInvoke(dispatchName.Name, instance, args, out resolved));
        result = resolved;
        return success;
    }

    internal static bool TryInvokeStatic(
        AlderConfig config, Type type, string name,
        object?[] args, out object? result)
    {
        object? resolved = null;
        var success = TryDispatchNamed(
            config, type, name, isStatic: true, MemberTypes.Method,
            dispatch => dispatch.TryInvokeStatic(name, args, out resolved),
            dispatchName => dispatchName.Dispatch.TryInvokeStatic(dispatchName.Name, args, out resolved));
        result = resolved;
        return success;
    }

    internal static bool TryGetMember(
        AlderConfig config, Type type, string name,
        object instance, out object? value)
    {
        object? resolved = null;
        var success = TryDispatchNamed(
            config, type, name, isStatic: false, MemberTypes.Property | MemberTypes.Field,
            dispatch => TryGetMemberValue(dispatch, name, instance, "member", name, ref resolved),
            dispatchName => TryGetMemberValue(dispatchName.Dispatch, dispatchName.Name, instance, "member", name, ref resolved));
        value = resolved;
        return success;
    }

    internal static bool TryGetStaticMember(
        AlderConfig config, Type type, string name,
        out object? value)
    {
        object? resolved = null;
        var success = TryDispatchNamed(
            config, type, name, isStatic: true, MemberTypes.Property | MemberTypes.Field,
            dispatch => TryGetStaticMemberValue(dispatch, name, "static member", name, ref resolved),
            dispatchName => TryGetStaticMemberValue(dispatchName.Dispatch, dispatchName.Name, "static member", name, ref resolved));
        value = resolved;
        return success;
    }

    internal static bool TrySetMember(
        AlderConfig config, Type type, string name,
        object instance, object? value)
    {
        return TryDispatchNamed(
            config, type, name, isStatic: false, MemberTypes.Property | MemberTypes.Field,
            dispatch => dispatch.TrySet(name, instance, value),
            dispatchName => dispatchName.Dispatch.TrySet(dispatchName.Name, instance, value));
    }

    internal static bool TryGetIndex(
        AlderConfig config, Type type,
        object instance, object key, out object? value)
    {
        object? resolved = null;
        var success = TryDispatchChain(
            config,
            type,
            dispatch => TryGetIndexValue(dispatch, instance, key, ref resolved));
        value = resolved;
        return success;
    }

    internal static bool TrySetIndex(
        AlderConfig config, Type type,
        object instance, object key, object? value)
    {
        return TryDispatchChain(
            config,
            type,
            dispatch => dispatch.TrySetIndex(instance, key, value));
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

    private static bool TryDispatchChain(
        AlderConfig config,
        Type type,
        Func<TypedDispatch, bool> attempt)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (!config.TryGetDispatch(current, out var dispatch))
                continue;

            if (attempt(dispatch))
                return true;
        }

        return false;
    }

    private static bool TryDispatchNamed(
        AlderConfig config,
        Type type,
        string name,
        bool isStatic,
        MemberTypes memberTypes,
        Func<TypedDispatch, bool> tryOriginalName,
        Func<(TypedDispatch Dispatch, string Name), bool> tryCanonicalName)
    {
        return TryDispatchChain(
            config,
            type,
            dispatch =>
            {
                if (tryOriginalName(dispatch))
                    return true;

                var dispatchType = dispatch.Type;
                var canonicalName = ResolveCanonicalName(config, dispatchType, name, isStatic, memberTypes);
                return canonicalName != null && tryCanonicalName((dispatch, canonicalName));
            });
    }

    private static bool TryGetMemberValue(
        TypedDispatch dispatch,
        string dispatchName,
        object instance,
        string accessKind,
        string requestedName,
        ref object? value)
    {
        if (!dispatch.TryGet(dispatchName, instance, out value))
            return false;

        value = TypeHelpers.GuardReflectionLeak(value, accessKind, requestedName);
        return true;
    }

    private static bool TryGetStaticMemberValue(
        TypedDispatch dispatch,
        string dispatchName,
        string accessKind,
        string requestedName,
        ref object? value)
    {
        if (!dispatch.TryGetStatic(dispatchName, out value))
            return false;

        value = TypeHelpers.GuardReflectionLeak(value, accessKind, requestedName);
        return true;
    }

    private static bool TryGetIndexValue(
        TypedDispatch dispatch,
        object instance,
        object key,
        ref object? value)
    {
        if (!dispatch.TryGetIndex(instance, key, out value))
            return false;

        value = TypeHelpers.GuardReflectionLeak(value, "indexer");
        return true;
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
        if (config.IsCaseSensitive || !MethodDispatchCache.DynamicCodeSupported)
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
