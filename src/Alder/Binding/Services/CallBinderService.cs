using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder.Binding.Services;

internal sealed record CallBindResult(
    ResolvedCall Resolution,
    bool IsStaticCall,
    bool IsModuleCall = false)
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

    public bool TryBindStaticCall(Type declaringType, string methodName, IReadOnlyList<object?> args, bool isCaseSensitive, out CallBindResult? plan)
    {
        var flags = BindingFlags.Public | BindingFlags.Static;
        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var methods = _context.TypeMetadata.GetMethods(declaringType, methodName, flags);
        var sourceTypes = args.Select(static arg => arg?.GetType() ?? typeof(object)).ToArray();
        return TryBindFromTypes(methods, sourceTypes, isStaticCall: true, out plan, out _);
    }

    public CallBindResult BindStaticCall(Type declaringType, string methodName, IReadOnlyList<object?> args, bool isCaseSensitive)
    {
        var flags = BindingFlags.Public | BindingFlags.Static;
        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var methods = _context.TypeMetadata.GetMethods(declaringType, methodName, flags);
        var sourceTypes = args.Select(static arg => arg?.GetType() ?? typeof(object)).ToArray();
        return BindFromTypes(methods, sourceTypes, methodName, isStaticCall: true);
    }

    public bool TryBindInstanceCall(Type targetType, string methodName, IReadOnlyList<object?> args, bool isCaseSensitive, out CallBindResult? plan)
    {
        var flags = BindingFlags.Public | BindingFlags.Instance;
        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var methods = _context.TypeMetadata.GetMethods(targetType, methodName, flags);
        var sourceTypes = args.Select(static arg => arg?.GetType() ?? typeof(object)).ToArray();
        return TryBindFromTypes(methods, sourceTypes, isStaticCall: false, out plan, out _);
    }

    public CallBindResult BindInstanceCall(Type targetType, string methodName, IReadOnlyList<object?> args, bool isCaseSensitive)
    {
        var flags = BindingFlags.Public | BindingFlags.Instance;
        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var methods = _context.TypeMetadata.GetMethods(targetType, methodName, flags);
        var sourceTypes = args.Select(static arg => arg?.GetType() ?? typeof(object)).ToArray();
        return BindFromTypes(methods, sourceTypes, methodName, isStaticCall: false);
    }

    public bool TryBindStaticCall(
        Type declaringType,
        string methodName,
        IReadOnlyList<Type> argumentTypes,
        bool isCaseSensitive,
        out CallBindResult? plan)
    {
        var flags = BindingFlags.Public | BindingFlags.Static;
        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var methods = _context.TypeMetadata.GetMethods(declaringType, methodName, flags);
        return TryBindFromTypes(methods, argumentTypes, isStaticCall: true, out plan, out _);
    }

    public CallBindResult BindStaticCall(
        Type declaringType,
        string methodName,
        IReadOnlyList<Type> argumentTypes,
        bool isCaseSensitive)
    {
        var flags = BindingFlags.Public | BindingFlags.Static;
        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var methods = _context.TypeMetadata.GetMethods(declaringType, methodName, flags);
        return BindFromTypes(methods, argumentTypes, methodName, isStaticCall: true);
    }

    public bool TryBindInstanceCall(
        Type targetType,
        string methodName,
        IReadOnlyList<Type> argumentTypes,
        bool isCaseSensitive,
        out CallBindResult? plan)
    {
        var flags = BindingFlags.Public | BindingFlags.Instance;
        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var methods = _context.TypeMetadata.GetMethods(targetType, methodName, flags);
        return TryBindFromTypes(methods, argumentTypes, isStaticCall: false, out plan, out _);
    }

    public CallBindResult BindInstanceCall(
        Type targetType,
        string methodName,
        IReadOnlyList<Type> argumentTypes,
        bool isCaseSensitive)
    {
        var flags = BindingFlags.Public | BindingFlags.Instance;
        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var methods = _context.TypeMetadata.GetMethods(targetType, methodName, flags);
        return BindFromTypes(methods, argumentTypes, methodName, isStaticCall: false);
    }

    private static bool TryBindFromTypes(
        MethodInfo[] methods,
        IReadOnlyList<Type> sourceTypes,
        bool isStaticCall,
        out CallBindResult? plan,
        out bool isAmbiguous)
    {
        plan = null;
        isAmbiguous = false;

        if (methods.Length == 0)
            return false;

        if (methods.Length > 1 && sourceTypes.Any(static sourceType => sourceType == typeof(object)))
            return false;

        var argTypes = sourceTypes is Type[] arr ? arr : sourceTypes.ToArray();
        var descriptors = ArgumentDescriptor.FromTypes(argTypes);

        if (!OverloadResolver.TryResolve(methods, descriptors, context: null, out var resolved, out isAmbiguous))
            return false;

        if (resolved.Method.ContainsGenericParameters)
            return false;

        var parameters = MethodDispatchCache.GetParameters(resolved.Method);
        if (parameters.Any(static parameter => parameter.ParameterType.IsByRef))
            return false;

        plan = new CallBindResult(resolved, isStaticCall);
        return true;
    }

    private static CallBindResult BindFromTypes(
        MethodInfo[] methods,
        IReadOnlyList<Type> sourceTypes,
        string methodName,
        bool isStaticCall)
    {
        if (methods.Length == 0)
            throw new AlderException(DiagnosticDescriptors.MemberNotFound, "type", methodName);

        if (methods.Length > 1 && sourceTypes.Any(static sourceType => sourceType == typeof(object)))
            throw new AlderException(DiagnosticDescriptors.RuntimeOverloadResolutionRequired, methodName);

        if (TryBindFromTypes(methods, sourceTypes, isStaticCall, out var plan, out var isAmbiguous))
            return plan!;

        if (isAmbiguous)
            throw new AlderException(DiagnosticDescriptors.AmbiguousMethodInvocation, methodName);

        throw new AlderException(DiagnosticDescriptors.NoApplicableOverload, methodName);
    }

}
