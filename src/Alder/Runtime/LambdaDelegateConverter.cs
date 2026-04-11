using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Alder.Diagnostics;
using Alder.Runtime.Collections;

namespace Alder.Runtime;

/// <summary>
/// Converts Alder lambda values (LambdaValue, CompiledLambdaValue) to System.Func/Action delegates.
/// </summary>
internal static class LambdaDelegateConverter
{
    private static readonly FixedSet<Type> SupportedFuncDefinitions = CreateOpenGenericDelegateSet("Func", 1, 17);
    private static readonly FixedSet<Type> SupportedActionDefinitions = CreateOpenGenericDelegateSet("Action", 1, 16);

    private static readonly ConditionalWeakTable<object, ConcurrentDictionary<Type, Delegate>> DelegateCache = new();

    public static Delegate? TryConvert(object value, Type delegateType)
    {
        if (!typeof(Delegate).IsAssignableFrom(delegateType))
            return null;

        return value switch
        {
            CompiledLambdaValue compiled => ConvertLambda(compiled, compiled.Parameters.Count, delegateType),
            LambdaValue interpreted => ConvertLambda(interpreted, interpreted.Parameters.Count, delegateType),
            StaticMethodRef staticRef => ConvertMethodRef(staticRef.Type, staticRef.MethodName, null, delegateType),
            ModuleMethodRef moduleRef => ConvertMethodRef(moduleRef.Method.DeclaringType!, moduleRef.Method.Name, moduleRef.Module.Instance, delegateType),
            MethodRef { Target: Type staticType } methodRef => ConvertMethodRef(staticType, methodRef.MethodName, null, delegateType),
            MethodRef instanceRef => ConvertMethodRef(instanceRef.Target.GetType(), instanceRef.MethodName, instanceRef.Target, delegateType),
            _ => null
        };
    }

    internal static bool IsSupportedDelegateType(Type type)
    {
        if (type == typeof(Action))
            return true;

        if (!type.IsGenericType || type.ContainsGenericParameters)
            return false;

        var delegateDefinition = type.GetGenericTypeDefinition();
        return SupportedFuncDefinitions.Contains(delegateDefinition) ||
               SupportedActionDefinitions.Contains(delegateDefinition);
    }

    /// <summary>
    /// Extracts (parameterTypes, returnType) from any delegate type via its Invoke method.
    /// ECMA-334 §20.1: all delegate types have an Invoke method with matching signature.
    /// </summary>
    private static (Type[]? ParamTypes, Type ReturnType) GetDelegateSignature(Type delegateType)
    {
        var invoke = delegateType.GetMethod(nameof(Action.Invoke));
        if (invoke == null)
            return (null, typeof(void));

        var parameters = invoke.GetParameters();
        var paramTypes = new Type[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
            paramTypes[i] = parameters[i].ParameterType;

        return (paramTypes, invoke.ReturnType);
    }

    private static Delegate? ConvertLambda(object lambda, int paramCount, Type delegateType)
    {
        var (paramTypes, returnType) = GetDelegateSignature(delegateType);
        if (paramTypes == null || paramCount != paramTypes.Length)
            return null;

        // Build Func<>/Action<> equivalent for the factory
        var funcActionType = IsSupportedDelegateType(delegateType) ? delegateType : BuildFuncActionType(paramTypes, returnType);
        if (funcActionType == null)
            return null;

        var typeCache = DelegateCache.GetOrCreateValue(lambda);
        return typeCache.GetOrAdd(delegateType, _ =>
        {
            var delegateFactories = lambda switch
            {
                CompiledLambdaValue compiledLambda => compiledLambda.Closure.Config.DelegateFactories,
                LambdaValue interpretedLambda => interpretedLambda.Closure.Config.DelegateFactories,
                _ => null
            };

            Delegate inner;
            if (lambda is CompiledLambdaValue compiled)
                inner = LambdaDelegateFactory.CreateCompiledDelegate(compiled, funcActionType, paramTypes, returnType);
            else
            {
                var interpreted = (LambdaValue)lambda;
                if (delegateFactories != null && delegateFactories.TryGetValue(funcActionType, out var aotFactory))
                    inner = aotFactory(interpreted);
                else
                    inner = LambdaDelegateFactory.CreateInterpretedDelegate(interpreted, funcActionType, paramTypes, returnType);
            }

            if (funcActionType == delegateType)
                return inner;

            // §10.8: Rewrap for arbitrary delegate types (Comparison<T>, Predicate<T>, etc.)
            return Delegate.CreateDelegate(delegateType, inner.Target, inner.Method);
        });
    }

    private static Delegate? ConvertMethodRef(Type declaringType, string methodName, object? target, Type delegateType)
    {
        var (paramTypes, _) = GetDelegateSignature(delegateType);
        if (paramTypes == null)
            return null;

        var flags = target != null
            ? BindingFlags.Public | BindingFlags.Instance
            : BindingFlags.Public | BindingFlags.Static;
        var method = declaringType.GetMethod(methodName, flags, null, paramTypes, null);
        if (method == null)
            return null;

        return target != null
            ? Delegate.CreateDelegate(delegateType, target, method)
            : Delegate.CreateDelegate(delegateType, method);
    }

    private static Type? BuildFuncActionType(Type[] paramTypes, Type returnType)
    {
        if (returnType == typeof(void))
        {
            if (paramTypes.Length == 0) return typeof(Action);
            var openAction = GetOpenActionType(paramTypes.Length);
            return openAction?.MakeGenericType(paramTypes);
        }

        var openFunc = GetOpenFuncType(paramTypes.Length + 1);
        if (openFunc == null) return null;
        var genericArgs = new Type[paramTypes.Length + 1];
        Array.Copy(paramTypes, genericArgs, paramTypes.Length);
        genericArgs[^1] = returnType;
        return openFunc.MakeGenericType(genericArgs);
    }

    private static FixedSet<Type> CreateOpenGenericDelegateSet(string delegateName, int minArity, int maxArity)
    {
        var definitions = new HashSet<Type>();
        for (var arity = minArity; arity <= maxArity; arity++)
        {
            Type? openGeneric = delegateName switch
            {
                "Func" => GetOpenFuncType(arity),
                "Action" => GetOpenActionType(arity),
                _ => null
            };
            if (openGeneric == null)
            {
                throw new AlderException(DiagnosticDescriptors.DelegateTypeDefinitionNotFound,
                    $"System.{delegateName}`{arity}");
            }

            definitions.Add(openGeneric);
        }

        return FixedSet<Type>.Create(definitions);
    }

    private static Type? GetOpenFuncType(int arity) => arity switch
    {
        1 => typeof(Func<>),
        2 => typeof(Func<,>),
        3 => typeof(Func<,,>),
        4 => typeof(Func<,,,>),
        5 => typeof(Func<,,,,>),
        6 => typeof(Func<,,,,,>),
        7 => typeof(Func<,,,,,,>),
        8 => typeof(Func<,,,,,,,>),
        9 => typeof(Func<,,,,,,,,>),
        10 => typeof(Func<,,,,,,,,,>),
        11 => typeof(Func<,,,,,,,,,,>),
        12 => typeof(Func<,,,,,,,,,,,>),
        13 => typeof(Func<,,,,,,,,,,,,>),
        14 => typeof(Func<,,,,,,,,,,,,,>),
        15 => typeof(Func<,,,,,,,,,,,,,,>),
        16 => typeof(Func<,,,,,,,,,,,,,,,>),
        17 => typeof(Func<,,,,,,,,,,,,,,,,>),
        _ => null
    };

    private static Type? GetOpenActionType(int arity) => arity switch
    {
        1 => typeof(Action<>),
        2 => typeof(Action<,>),
        3 => typeof(Action<,,>),
        4 => typeof(Action<,,,>),
        5 => typeof(Action<,,,,>),
        6 => typeof(Action<,,,,,>),
        7 => typeof(Action<,,,,,,>),
        8 => typeof(Action<,,,,,,,>),
        9 => typeof(Action<,,,,,,,,>),
        10 => typeof(Action<,,,,,,,,,>),
        11 => typeof(Action<,,,,,,,,,,>),
        12 => typeof(Action<,,,,,,,,,,,>),
        13 => typeof(Action<,,,,,,,,,,,,>),
        14 => typeof(Action<,,,,,,,,,,,,,>),
        15 => typeof(Action<,,,,,,,,,,,,,,>),
        16 => typeof(Action<,,,,,,,,,,,,,,,>),
        _ => null
    };
}
