using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Alder.Runtime;

// Invoker methods are in MethodDispatchCache.generated.cs (scripts/generate-fast-invokers.sh)
internal static partial class MethodDispatchCache
{
    internal delegate object? FastInvoker(object? target, object?[] args);

    private const int MaxFastInvokerArity = 8;

    private static readonly ConcurrentDictionary<MethodInfo, ParameterInfo[]> ParameterCache = new();
    private static readonly ConcurrentDictionary<MethodInfo, FastInvoker?> FastInvokerCache = new();

    private static readonly MethodInfo[] StaticVoidFactories = BuildFactoryArray("CreateStaticVoidInvoker", MaxFastInvokerArity + 1);
    private static readonly MethodInfo[] StaticFactories = BuildFactoryArray("CreateStaticInvoker", MaxFastInvokerArity + 1);
    private static readonly MethodInfo[] InstanceVoidFactories = BuildFactoryArray("CreateInstanceVoidInvoker", MaxFastInvokerArity + 1);
    private static readonly MethodInfo[] InstanceFactories = BuildFactoryArray("CreateInstanceInvoker", MaxFastInvokerArity + 1);

    internal static readonly bool DynamicCodeSupported =
#if NET7_0_OR_GREATER
        RuntimeFeature.IsDynamicCodeSupported;
#else
        true;
#endif

    internal static ParameterInfo[] GetParameters(MethodInfo method) =>
        ParameterCache.GetOrAdd(method, static m => m.GetParameters());

    internal static bool TryInvokeFast(MethodInfo method, object? target, object?[] args, out object? result)
    {
        if (!DynamicCodeSupported)
        {
            result = null;
            return false;
        }

        var invoker = FastInvokerCache.GetOrAdd(method, static m => CreateFastInvoker(m));
        if (invoker == null)
        {
            result = null;
            return false;
        }

        result = invoker(target, args);
        return true;
    }

    private static FastInvoker? CreateFastInvoker(MethodInfo method)
    {
        var declaringType = method.DeclaringType;
        if (declaringType == null)
            return null;

        if (!method.IsStatic && declaringType.IsValueType)
            return null;

        if (method.ReturnType.IsByRef)
            return null;

        var parameters = GetParameters(method);
        if (parameters.Length > MaxFastInvokerArity)
            return null;

        foreach (var parameter in parameters)
        {
            if (parameter.ParameterType.IsByRef || parameter.IsDefined(typeof(ParamArrayAttribute), false))
                return null;
        }

        return CreateFastInvoker(method, declaringType, parameters);
    }

    private static FastInvoker CreateFastInvoker(MethodInfo method, Type declaringType, ParameterInfo[] parameters)
    {
        return method.IsStatic
            ? CreateStaticFastInvoker(method, parameters)
            : CreateInstanceFastInvoker(method, declaringType, parameters);
    }

    private static FastInvoker CreateStaticFastInvoker(MethodInfo method, ParameterInfo[] parameters)
    {
        var isVoid = method.ReturnType == typeof(void);
        var parameterTypes = parameters.Select(static p => p.ParameterType).ToArray();

        var factories = isVoid ? StaticVoidFactories : StaticFactories;
        var genericArgs = isVoid
            ? parameterTypes
            : [.. parameterTypes, method.ReturnType];

        return CloseFactory(factories[parameters.Length], genericArgs, method);
    }

    private static FastInvoker CreateInstanceFastInvoker(MethodInfo method, Type declaringType, ParameterInfo[] parameters)
    {
        var isVoid = method.ReturnType == typeof(void);
        var parameterTypes = parameters.Select(static p => p.ParameterType).ToArray();

        var factories = isVoid ? InstanceVoidFactories : InstanceFactories;
        Type[] genericArgs;
        if (isVoid)
        {
            genericArgs = new Type[1 + parameterTypes.Length];
            genericArgs[0] = declaringType;
            Array.Copy(parameterTypes, 0, genericArgs, 1, parameterTypes.Length);
        }
        else
        {
            genericArgs = new Type[2 + parameterTypes.Length];
            genericArgs[0] = declaringType;
            Array.Copy(parameterTypes, 0, genericArgs, 1, parameterTypes.Length);
            genericArgs[^1] = method.ReturnType;
        }

        return CloseFactory(factories[parameters.Length], genericArgs, method);
    }

    private static FastInvoker CloseFactory(MethodInfo factoryMethod, Type[] genericArgs, MethodInfo targetMethod)
    {
        var closedFactory = RuntimeGenericFactory.CloseGenericMethod(factoryMethod, genericArgs);
        return (FastInvoker)closedFactory.Invoke(null, [targetMethod])!;
    }

    private static MethodInfo[] BuildFactoryArray(string prefix, int count)
    {
        var result = new MethodInfo[count];
        for (var i = 0; i < count; i++)
            result[i] = typeof(MethodDispatchCache).GetMethod(
                $"{prefix}{i}", BindingFlags.NonPublic | BindingFlags.Static)!;
        return result;
    }
}
