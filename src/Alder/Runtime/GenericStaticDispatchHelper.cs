namespace Alder.Runtime;

internal static class GenericStaticDispatchHelper
{
    internal static bool TryInvoke(
        AlderConfig config,
        Type declaringType,
        string methodName,
        object?[] args,
        IReadOnlyList<string>? typeArgs,
        TypeResolver resolver,
        MethodInfo? resolvedMethod,
        out object? result)
    {
        if (config.GenericStaticDispatch is not { } dispatchers ||
            !dispatchers.TryGetValue(declaringType, out var entries))
        {
            result = null;
            return false;
        }

        var requestedType = ResolveRequestedType(typeArgs, args, resolver, resolvedMethod);
        foreach (var entry in entries)
        {
            if (entry.TryInvoke(methodName, requestedType, args, out result))
                return true;
        }

        result = null;
        return false;
    }

    private static Type? ResolveRequestedType(
        IReadOnlyList<string>? typeArgs,
        object?[] args,
        TypeResolver resolver,
        MethodInfo? resolvedMethod)
    {
        if (typeArgs is { Count: 1 })
            return resolver.TryResolveType(typeArgs[0]);

        if (resolvedMethod is { IsGenericMethodDefinition: false, IsGenericMethod: true })
        {
            var methodArgs = resolvedMethod.GetGenericArguments();
            if (methodArgs.Length == 1)
                return methodArgs[0];
        }

        return args.Length == 1 ? args[0]?.GetType() : null;
    }
}
