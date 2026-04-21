using Alder.Runtime;
using Alder.Runtime.OverloadResolution;

namespace Alder.Binding.Services;

internal sealed record CallBindResult(
    ResolvedCall Resolution,
    bool IsStaticCall,
    bool IsModuleCall = false,
    bool IsExtensionCall = false)
{
    public MethodInfo SelectedMethod => Resolution.Method;
}

internal sealed class CallBinderService
{
    private readonly AlderContext _context;

    public CallBinderService(AlderContext context)
    {
        _context = context;
    }

    public bool TryBindCall(
        Type declaringType,
        string methodName,
        ReadOnlySpan<ArgumentDescriptor> arguments,
        bool isStaticCall,
        bool isCaseSensitive,
        out CallBindResult? plan)
    {
        var flags = BindingFlags.Public | (isStaticCall ? BindingFlags.Static : BindingFlags.Instance);
        return TryBindCallCore(declaringType, methodName, flags, arguments, isStaticCall, isCaseSensitive, out plan);
    }

    public bool TryBindExtensionCall(
        Type targetType,
        string methodName,
        ReadOnlySpan<ArgumentDescriptor> userArguments,
        bool isCaseSensitive,
        out CallBindResult? plan)
    {
        plan = null;
        var extensionTypes = _context.ExtensionTypes;
        if (extensionTypes.IsDefaultOrEmpty)
            return false;

        var receiverDescriptor = ArgumentDescriptor.ForType(targetType);
        var receiverAndArgs = new ArgumentDescriptor[userArguments.Length + 1];
        receiverAndArgs[0] = receiverDescriptor;
        userArguments.CopyTo(receiverAndArgs.AsSpan(1));

        var invocationArgCount = receiverAndArgs.Length;

        return TryBindExtensionCallCore(
            extensionTypes,
            targetType,
            methodName,
            isCaseSensitive,
            invocationArgCount,
            receiverAndArgs,
            out plan);
    }

    private bool TryBindCallCore(
        Type declaringType,
        string methodName,
        BindingFlags flags,
        ReadOnlySpan<ArgumentDescriptor> arguments,
        bool isStaticCall,
        bool isCaseSensitive,
        out CallBindResult? plan)
    {
        plan = null;

        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var methods = _context.TypeMetadata.GetMethods(declaringType, methodName, flags);
        if (methods.Length == 0)
            return false;

        if (methods.Length > 1 && HasOpaqueObjectArgument(arguments))
            return false;

        if (!OverloadResolver.TryResolve(methods, arguments, context: _context, out var resolved, out _))
            return false;

        if (resolved.Method.ContainsGenericParameters)
            return false;

        var parameters = MethodDispatchCache.GetParameters(resolved.Method);
        if (parameters.Any(static parameter => parameter.ParameterType.IsByRef))
            return false;

        plan = new CallBindResult(resolved, isStaticCall);
        return true;
    }

    private bool TryBindExtensionCallCore(
        System.Collections.Immutable.ImmutableArray<Type> extensionTypes,
        Type targetType,
        string methodName,
        bool isCaseSensitive,
        int invocationArgCount,
        ArgumentDescriptor[] receiverAndArgs,
        out CallBindResult? plan)
    {
        plan = null;

        foreach (var extType in extensionTypes)
        {
            var methods = ExtensionMethodResolver.GetExtensionMethodsForArity(
                extType, methodName, isCaseSensitive, invocationArgCount);
            if (methods.Length == 0)
                continue;

            if (!OverloadResolver.TryResolveExtension(
                    methods, targetType, receiverAndArgs, _context,
                    out var resolved, out _))
                continue;

            if (resolved.Method.ContainsGenericParameters)
                continue;

            var parameters = MethodDispatchCache.GetParameters(resolved.Method);
            if (parameters.Any(static p => p.ParameterType.IsByRef))
                continue;

            plan = new CallBindResult(resolved, IsStaticCall: true, IsExtensionCall: true);
            return true;
        }

        return false;
    }

    private static bool HasOpaqueObjectArgument(ReadOnlySpan<ArgumentDescriptor> arguments)
    {
        foreach (var argument in arguments)
        {
            if (argument.Kind == ArgumentKind.Value && argument.StaticType == typeof(object))
                return true;
        }

        return false;
    }
}
