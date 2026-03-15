using System.Collections.Concurrent;
using CsEval.Diagnostics;

namespace CsEval.Runtime;

/// <summary>
/// Converts CsEval lambda values (LambdaValue, CompiledLambdaValue) to System.Func/Action delegates.
/// </summary>
internal static class LambdaDelegateConverter
{
    private static readonly HashSet<Type> SupportedFuncDefinitions = CreateOpenGenericDelegateSet("Func", 1, 17);
    private static readonly HashSet<Type> SupportedActionDefinitions = CreateOpenGenericDelegateSet("Action", 1, 16);

    // Cache generated delegate wrappers by (lambda identity, delegate type signature).
    private static readonly ConcurrentDictionary<(int LambdaId, Type DelegateType), Delegate> DelegateCache = new();

    /// <summary>
    /// Attempts to convert a lambda value to a specific delegate type.
    /// Returns the delegate if conversion succeeds, null otherwise.
    /// </summary>
    public static Delegate? TryConvert(object value, Type delegateType)
    {
        if (!IsSupportedDelegateType(delegateType))
            return null;

        return value switch
        {
            CompiledLambdaValue compiled => ConvertCompiledLambda(compiled, delegateType),
            LambdaValue interpreted => ConvertInterpretedLambda(interpreted, delegateType),
            _ => null
        };
    }

    /// <summary>
    /// Checks if a type is a supported delegate type (Func or Action).
    /// </summary>
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
    /// Converts a CompiledLambdaValue to a typed delegate.
    /// Uses caching to avoid rebuilding wrapper delegates.
    /// </summary>
    private static Delegate ConvertCompiledLambda(CompiledLambdaValue lambda, Type delegateType)
    {
        var (paramTypes, returnType) = GetDelegateSignature(delegateType);
        ValidateSignature(lambda.Parameters.Count, paramTypes.Length, delegateType);

        var cacheKey = (System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(lambda), delegateType);
        return DelegateCache.GetOrAdd(
            cacheKey,
            _ => LambdaDelegateFactory.CreateCompiledDelegate(lambda, delegateType, paramTypes, returnType));
    }

    /// <summary>
    /// Converts a LambdaValue (interpreted) to a typed delegate.
    /// Uses caching to avoid rebuilding wrapper delegates.
    /// </summary>
    private static Delegate ConvertInterpretedLambda(LambdaValue lambda, Type delegateType)
    {
        var (paramTypes, returnType) = GetDelegateSignature(delegateType);
        ValidateSignature(lambda.Parameters.Count, paramTypes.Length, delegateType);

        var cacheKey = (System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(lambda), delegateType);
        return DelegateCache.GetOrAdd(
            cacheKey,
            _ => LambdaDelegateFactory.CreateInterpretedDelegate(lambda, delegateType, paramTypes, returnType));
    }

    /// <summary>
    /// Extracts parameter types and return type from a Func/Action delegate type.
    /// </summary>
    private static (Type[] ParamTypes, Type ReturnType) GetDelegateSignature(Type delegateType)
    {
        if (!TryGetDelegateKind(delegateType, out var isFunc))
        {
            throw new CsEvalException(
                DiagnosticDescriptors.DelegateConversionFailed, delegateType.Name, "Func<> or Action<>");
        }

        var genericArgs = delegateType.GetGenericArguments();

        if (isFunc)
        {
            // Func<T1, T2, ..., TResult>: last arg is return type
            var paramTypes = genericArgs.Take(genericArgs.Length - 1).ToArray();
            var returnType = genericArgs[^1];
            return (paramTypes, returnType);
        }

        // Action<T1, T2, ...>: all args are parameters, void return
        return (genericArgs, typeof(void));
    }

    /// <summary>
    /// Validates that lambda parameter count matches delegate signature.
    /// </summary>
    private static void ValidateSignature(int lambdaParamCount, int delegateParamCount, Type delegateType)
    {
        if (lambdaParamCount != delegateParamCount)
        {
            throw new CsEvalException(
                DiagnosticDescriptors.DelegateConversionFailed,
                $"lambda({lambdaParamCount} params)", $"{delegateType.Name}({delegateParamCount} params)");
        }
    }

    private static bool TryGetDelegateKind(Type delegateType, out bool isFunc)
    {
        isFunc = false;

        if (delegateType == typeof(Action))
            return true;

        if (!delegateType.IsGenericType || delegateType.ContainsGenericParameters)
            return false;

        var definition = delegateType.GetGenericTypeDefinition();
        if (SupportedFuncDefinitions.Contains(definition))
        {
            isFunc = true;
            return true;
        }

        if (SupportedActionDefinitions.Contains(definition))
            return true;

        return false;
    }

    private static HashSet<Type> CreateOpenGenericDelegateSet(string delegateName, int minArity, int maxArity)
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
                throw new InvalidOperationException(
                    $"Could not resolve delegate type definition for System.{delegateName}`{arity}.");
            }

            definitions.Add(openGeneric);
        }

        return definitions;
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
