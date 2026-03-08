using System.Collections.Concurrent;

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
                $"Unsupported delegate type '{delegateType}'. Only System.Func and System.Action are supported.");
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
                $"Cannot convert lambda with {lambdaParamCount} parameter(s) to delegate type '{delegateType.Name}' " +
                $"which expects {delegateParamCount} parameter(s)");
        }
    }

    private static bool TryGetDelegateKind(Type delegateType, out bool isFunc)
    {
        isFunc = false;

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
            var openGeneric = Type.GetType($"System.{delegateName}`{arity}");
            if (openGeneric == null)
            {
                throw new InvalidOperationException(
                    $"Could not resolve delegate type definition for System.{delegateName}`{arity}.");
            }

            definitions.Add(openGeneric);
        }

        return definitions;
    }
}
