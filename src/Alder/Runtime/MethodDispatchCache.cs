using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Alder.Runtime;

internal static class MethodDispatchCache
{
    internal delegate object? FastInvoker(object? target, object?[] args);

    private const int MaxFastInvokerArity = 4;

    private static readonly ConcurrentDictionary<MethodInfo, ParameterInfo[]> ParameterCache = new();
    private static readonly ConcurrentDictionary<MethodInfo, FastInvoker?> FastInvokerCache = new();

    private static readonly MethodInfo StaticVoidFactory0 = GetFactory(nameof(CreateStaticVoidInvoker0));
    private static readonly MethodInfo StaticVoidFactory1 = GetFactory(nameof(CreateStaticVoidInvoker1));
    private static readonly MethodInfo StaticVoidFactory2 = GetFactory(nameof(CreateStaticVoidInvoker2));
    private static readonly MethodInfo StaticVoidFactory3 = GetFactory(nameof(CreateStaticVoidInvoker3));
    private static readonly MethodInfo StaticVoidFactory4 = GetFactory(nameof(CreateStaticVoidInvoker4));

    private static readonly MethodInfo StaticFactory0 = GetFactory(nameof(CreateStaticInvoker0));
    private static readonly MethodInfo StaticFactory1 = GetFactory(nameof(CreateStaticInvoker1));
    private static readonly MethodInfo StaticFactory2 = GetFactory(nameof(CreateStaticInvoker2));
    private static readonly MethodInfo StaticFactory3 = GetFactory(nameof(CreateStaticInvoker3));
    private static readonly MethodInfo StaticFactory4 = GetFactory(nameof(CreateStaticInvoker4));

    private static readonly MethodInfo InstanceVoidFactory0 = GetFactory(nameof(CreateInstanceVoidInvoker0));
    private static readonly MethodInfo InstanceVoidFactory1 = GetFactory(nameof(CreateInstanceVoidInvoker1));
    private static readonly MethodInfo InstanceVoidFactory2 = GetFactory(nameof(CreateInstanceVoidInvoker2));
    private static readonly MethodInfo InstanceVoidFactory3 = GetFactory(nameof(CreateInstanceVoidInvoker3));
    private static readonly MethodInfo InstanceVoidFactory4 = GetFactory(nameof(CreateInstanceVoidInvoker4));

    private static readonly MethodInfo InstanceFactory0 = GetFactory(nameof(CreateInstanceInvoker0));
    private static readonly MethodInfo InstanceFactory1 = GetFactory(nameof(CreateInstanceInvoker1));
    private static readonly MethodInfo InstanceFactory2 = GetFactory(nameof(CreateInstanceInvoker2));
    private static readonly MethodInfo InstanceFactory3 = GetFactory(nameof(CreateInstanceInvoker3));
    private static readonly MethodInfo InstanceFactory4 = GetFactory(nameof(CreateInstanceInvoker4));

    internal static ParameterInfo[] GetParameters(MethodInfo method) =>
        ParameterCache.GetOrAdd(method, static m => m.GetParameters());

    internal static bool TryInvokeFast(MethodInfo method, object? target, object?[] args, out object? result)
    {
#if NET7_0_OR_GREATER
        if (!RuntimeFeature.IsDynamicCodeSupported)
#else
        if (false) // dynamic code always supported on non-AOT runtimes
#endif
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

        // Keep value-type instance methods on MethodInfo.Invoke to preserve boxed struct semantics.
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

        return parameters.Length switch
        {
            0 when isVoid => CloseFactory(StaticVoidFactory0, [], method),
            1 when isVoid => CloseFactory(StaticVoidFactory1, parameterTypes, method),
            2 when isVoid => CloseFactory(StaticVoidFactory2, parameterTypes, method),
            3 when isVoid => CloseFactory(StaticVoidFactory3, parameterTypes, method),
            4 when isVoid => CloseFactory(StaticVoidFactory4, parameterTypes, method),
            0 => CloseFactory(StaticFactory0, [method.ReturnType], method),
            1 => CloseFactory(StaticFactory1, [.. parameterTypes, method.ReturnType], method),
            2 => CloseFactory(StaticFactory2, [.. parameterTypes, method.ReturnType], method),
            3 => CloseFactory(StaticFactory3, [.. parameterTypes, method.ReturnType], method),
            4 => CloseFactory(StaticFactory4, [.. parameterTypes, method.ReturnType], method),
            _ => throw new InvalidOperationException($"Unsupported fast static method arity: {parameters.Length}")
        };
    }

    private static FastInvoker CreateInstanceFastInvoker(MethodInfo method, Type declaringType, ParameterInfo[] parameters)
    {
        var isVoid = method.ReturnType == typeof(void);
        var parameterTypes = parameters.Select(static p => p.ParameterType).ToArray();

        return parameters.Length switch
        {
            0 when isVoid => CloseFactory(InstanceVoidFactory0, [declaringType], method),
            1 when isVoid => CloseFactory(InstanceVoidFactory1, [declaringType, .. parameterTypes], method),
            2 when isVoid => CloseFactory(InstanceVoidFactory2, [declaringType, .. parameterTypes], method),
            3 when isVoid => CloseFactory(InstanceVoidFactory3, [declaringType, .. parameterTypes], method),
            4 when isVoid => CloseFactory(InstanceVoidFactory4, [declaringType, .. parameterTypes], method),
            0 => CloseFactory(InstanceFactory0, [declaringType, method.ReturnType], method),
            1 => CloseFactory(InstanceFactory1, [declaringType, .. parameterTypes, method.ReturnType], method),
            2 => CloseFactory(InstanceFactory2, [declaringType, .. parameterTypes, method.ReturnType], method),
            3 => CloseFactory(InstanceFactory3, [declaringType, .. parameterTypes, method.ReturnType], method),
            4 => CloseFactory(InstanceFactory4, [declaringType, .. parameterTypes, method.ReturnType], method),
            _ => throw new InvalidOperationException($"Unsupported fast instance method arity: {parameters.Length}")
        };
    }

    private static FastInvoker CloseFactory(MethodInfo factoryMethod, Type[] genericArgs, MethodInfo targetMethod)
    {
        var closedFactory = RuntimeGenericFactory.CloseGenericMethod(factoryMethod, genericArgs);
        return (FastInvoker)closedFactory.Invoke(null, [targetMethod])!;
    }

    private static MethodInfo GetFactory(string name)
    {
        return typeof(MethodDispatchCache).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;
    }

    private static FastInvoker CreateStaticVoidInvoker0(MethodInfo method)
    {
        var action = (Action)method.CreateDelegate(typeof(Action));
        return (_, _) =>
        {
            action();
            return null;
        };
    }

    private static FastInvoker CreateStaticVoidInvoker1<T0>(MethodInfo method)
    {
        var action = (Action<T0>)method.CreateDelegate(typeof(Action<T0>));
        return (_, args) =>
        {
            action((T0)args[0]!);
            return null;
        };
    }

    private static FastInvoker CreateStaticVoidInvoker2<T0, T1>(MethodInfo method)
    {
        var action = (Action<T0, T1>)method.CreateDelegate(typeof(Action<T0, T1>));
        return (_, args) =>
        {
            action((T0)args[0]!, (T1)args[1]!);
            return null;
        };
    }

    private static FastInvoker CreateStaticVoidInvoker3<T0, T1, T2>(MethodInfo method)
    {
        var action = (Action<T0, T1, T2>)method.CreateDelegate(typeof(Action<T0, T1, T2>));
        return (_, args) =>
        {
            action((T0)args[0]!, (T1)args[1]!, (T2)args[2]!);
            return null;
        };
    }

    private static FastInvoker CreateStaticVoidInvoker4<T0, T1, T2, T3>(MethodInfo method)
    {
        var action = (Action<T0, T1, T2, T3>)method.CreateDelegate(typeof(Action<T0, T1, T2, T3>));
        return (_, args) =>
        {
            action((T0)args[0]!, (T1)args[1]!, (T2)args[2]!, (T3)args[3]!);
            return null;
        };
    }

    private static FastInvoker CreateStaticInvoker0<TResult>(MethodInfo method)
    {
        var func = (Func<TResult>)method.CreateDelegate(typeof(Func<TResult>));
        return (_, _) => func();
    }

    private static FastInvoker CreateStaticInvoker1<T0, TResult>(MethodInfo method)
    {
        var func = (Func<T0, TResult>)method.CreateDelegate(typeof(Func<T0, TResult>));
        return (_, args) => func((T0)args[0]!);
    }

    private static FastInvoker CreateStaticInvoker2<T0, T1, TResult>(MethodInfo method)
    {
        var func = (Func<T0, T1, TResult>)method.CreateDelegate(typeof(Func<T0, T1, TResult>));
        return (_, args) => func((T0)args[0]!, (T1)args[1]!);
    }

    private static FastInvoker CreateStaticInvoker3<T0, T1, T2, TResult>(MethodInfo method)
    {
        var func = (Func<T0, T1, T2, TResult>)method.CreateDelegate(typeof(Func<T0, T1, T2, TResult>));
        return (_, args) => func((T0)args[0]!, (T1)args[1]!, (T2)args[2]!);
    }

    private static FastInvoker CreateStaticInvoker4<T0, T1, T2, T3, TResult>(MethodInfo method)
    {
        var func = (Func<T0, T1, T2, T3, TResult>)method.CreateDelegate(typeof(Func<T0, T1, T2, T3, TResult>));
        return (_, args) => func((T0)args[0]!, (T1)args[1]!, (T2)args[2]!, (T3)args[3]!);
    }

    private static FastInvoker CreateInstanceVoidInvoker0<TTarget>(MethodInfo method)
    {
        var action = (Action<TTarget>)method.CreateDelegate(typeof(Action<TTarget>));
        return (target, _) =>
        {
            action((TTarget)target!);
            return null;
        };
    }

    private static FastInvoker CreateInstanceVoidInvoker1<TTarget, T0>(MethodInfo method)
    {
        var action = (Action<TTarget, T0>)method.CreateDelegate(typeof(Action<TTarget, T0>));
        return (target, args) =>
        {
            action((TTarget)target!, (T0)args[0]!);
            return null;
        };
    }

    private static FastInvoker CreateInstanceVoidInvoker2<TTarget, T0, T1>(MethodInfo method)
    {
        var action = (Action<TTarget, T0, T1>)method.CreateDelegate(typeof(Action<TTarget, T0, T1>));
        return (target, args) =>
        {
            action((TTarget)target!, (T0)args[0]!, (T1)args[1]!);
            return null;
        };
    }

    private static FastInvoker CreateInstanceVoidInvoker3<TTarget, T0, T1, T2>(MethodInfo method)
    {
        var action = (Action<TTarget, T0, T1, T2>)method.CreateDelegate(typeof(Action<TTarget, T0, T1, T2>));
        return (target, args) =>
        {
            action((TTarget)target!, (T0)args[0]!, (T1)args[1]!, (T2)args[2]!);
            return null;
        };
    }

    private static FastInvoker CreateInstanceVoidInvoker4<TTarget, T0, T1, T2, T3>(MethodInfo method)
    {
        var action = (Action<TTarget, T0, T1, T2, T3>)method.CreateDelegate(typeof(Action<TTarget, T0, T1, T2, T3>));
        return (target, args) =>
        {
            action((TTarget)target!, (T0)args[0]!, (T1)args[1]!, (T2)args[2]!, (T3)args[3]!);
            return null;
        };
    }

    private static FastInvoker CreateInstanceInvoker0<TTarget, TResult>(MethodInfo method)
    {
        var func = (Func<TTarget, TResult>)method.CreateDelegate(typeof(Func<TTarget, TResult>));
        return (target, _) => func((TTarget)target!);
    }

    private static FastInvoker CreateInstanceInvoker1<TTarget, T0, TResult>(MethodInfo method)
    {
        var func = (Func<TTarget, T0, TResult>)method.CreateDelegate(typeof(Func<TTarget, T0, TResult>));
        return (target, args) => func((TTarget)target!, (T0)args[0]!);
    }

    private static FastInvoker CreateInstanceInvoker2<TTarget, T0, T1, TResult>(MethodInfo method)
    {
        var func = (Func<TTarget, T0, T1, TResult>)method.CreateDelegate(typeof(Func<TTarget, T0, T1, TResult>));
        return (target, args) => func((TTarget)target!, (T0)args[0]!, (T1)args[1]!);
    }

    private static FastInvoker CreateInstanceInvoker3<TTarget, T0, T1, T2, TResult>(MethodInfo method)
    {
        var func = (Func<TTarget, T0, T1, T2, TResult>)method.CreateDelegate(typeof(Func<TTarget, T0, T1, T2, TResult>));
        return (target, args) => func((TTarget)target!, (T0)args[0]!, (T1)args[1]!, (T2)args[2]!);
    }

    private static FastInvoker CreateInstanceInvoker4<TTarget, T0, T1, T2, T3, TResult>(MethodInfo method)
    {
        var func = (Func<TTarget, T0, T1, T2, T3, TResult>)method.CreateDelegate(typeof(Func<TTarget, T0, T1, T2, T3, TResult>));
        return (target, args) => func((TTarget)target!, (T0)args[0]!, (T1)args[1]!, (T2)args[2]!, (T3)args[3]!);
    }
}
