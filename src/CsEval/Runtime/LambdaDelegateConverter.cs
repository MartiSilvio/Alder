using System.Collections.Concurrent;
using CsEval.Diagnostics;
using CsEval.Interpretation;

namespace CsEval.Runtime;

/// <summary>
/// Converts CsEval lambda values (LambdaValue, CompiledLambdaValue) to System.Func/Action delegates.
/// </summary>
internal static class LambdaDelegateConverter
{
    // Cache compiled delegate wrappers by (lambda identity, delegate type signature)
    // Key: lambda hashcode + delegate type, Value: compiled delegate
    private static readonly ConcurrentDictionary<(int, Type), Delegate> _delegateCache = new();

    /// <summary>
    /// Attempts to convert a lambda value to a specific delegate type.
    /// Returns the delegate if conversion succeeds, null otherwise.
    /// </summary>
    public static Delegate? TryConvert(object value, Type delegateType)
    {
        if (!IsSupportedDelegateType(delegateType))
            return null;

        if (value is CompiledLambdaValue compiled)
            return ConvertCompiledLambda(compiled, delegateType);

        if (value is LambdaValue interpreted)
            return ConvertInterpretedLambda(interpreted, delegateType);

        return null;
    }

    /// <summary>
    /// Checks if a type is a supported delegate type (Func or Action).
    /// </summary>
    internal static bool IsSupportedDelegateType(Type type)
    {
        if (!type.IsGenericType || type.ContainsGenericParameters)
            return false;

        var fullName = type.GetGenericTypeDefinition().FullName;
        return fullName?.StartsWith("System.Func`") == true ||
               fullName?.StartsWith("System.Action`") == true;
    }

    /// <summary>
    /// Converts a CompiledLambdaValue to a typed delegate.
    /// Uses caching to avoid recompiling the same wrapper multiple times.
    /// </summary>
    private static Delegate ConvertCompiledLambda(CompiledLambdaValue lambda, Type delegateType)
    {
        var cacheKey = (lambda.GetHashCode(), delegateType);

        return _delegateCache.GetOrAdd(cacheKey, _ =>
        {
            var (paramTypes, returnType) = GetDelegateSignature(delegateType);
            ValidateSignature(lambda.Parameters.Count, paramTypes.Length, delegateType);

            return CreateCompiledLambdaWrapper(lambda, delegateType, paramTypes, returnType);
        });
    }

    /// <summary>
    /// Converts a LambdaValue (interpreted) to a typed delegate.
    /// Uses caching to avoid recompiling the same wrapper multiple times.
    /// </summary>
    private static Delegate ConvertInterpretedLambda(LambdaValue lambda, Type delegateType)
    {
        var cacheKey = (lambda.GetHashCode(), delegateType);

        return _delegateCache.GetOrAdd(cacheKey, _ =>
        {
            var (paramTypes, returnType) = GetDelegateSignature(delegateType);
            ValidateSignature(lambda.Parameters.Count, paramTypes.Length, delegateType);

            return CreateInterpretedLambdaWrapper(lambda, delegateType, paramTypes, returnType);
        });
    }

    /// <summary>
    /// Extracts parameter types and return type from a Func/Action delegate type.
    /// </summary>
    private static (Type[] ParamTypes, Type ReturnType) GetDelegateSignature(Type delegateType)
    {
        var genericArgs = delegateType.GetGenericArguments();
        var isFunc = delegateType.GetGenericTypeDefinition().FullName?.StartsWith("System.Func`") == true;

        if (isFunc)
        {
            // Func<T1, T2, ..., TResult>: last arg is return type
            var paramTypes = genericArgs.Take(genericArgs.Length - 1).ToArray();
            var returnType = genericArgs[^1];
            return (paramTypes, returnType);
        }
        else
        {
            // Action<T1, T2, ...>: all args are parameters, void return
            return (genericArgs, typeof(void));
        }
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

    /// <summary>
    /// Creates a typed delegate wrapper for a CompiledLambdaValue using Expression Trees.
    /// The wrapper: takes typed params → boxes to object?[] → calls CompiledBody → unboxes result.
    /// </summary>
    private static Delegate CreateCompiledLambdaWrapper(
        CompiledLambdaValue lambda,
        Type delegateType,
        Type[] paramTypes,
        Type returnType)
    {
        // Create typed parameters matching the delegate signature
        var parameters = paramTypes.Select((t, i) => LinqExpression.Parameter(t, $"p{i}")).ToArray();

        // Box parameters into object?[] for lambda invocation
        var argsArray = LinqExpression.NewArrayInit(
            typeof(object),
            parameters.Select(p => LinqExpression.Convert(p, typeof(object))));

        // Get the InvokeCompiledLambda method
        var invokeMethod = typeof(MethodInvoker).GetMethod(
            nameof(MethodInvoker.InvokeCompiledLambda),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;

        // Call: MethodInvoker.InvokeCompiledLambda(lambda, args)
        var lambdaConst = LinqExpression.Constant(lambda);
        var invokeCall = LinqExpression.Call(invokeMethod, lambdaConst, argsArray);

        // Handle return type (void for Action, typed for Func)
        LinqExpression body;
        if (returnType == typeof(void))
        {
            // Action: ignore return value
            body = LinqExpression.Block(invokeCall, LinqExpression.Empty());
        }
        else
        {
            // Func: convert result to expected return type
            body = LinqExpression.Convert(invokeCall, returnType);
        }

        // Compile the wrapper lambda to a delegate
        var lambdaExpr = LinqExpression.Lambda(delegateType, body, parameters);
        return lambdaExpr.Compile();
    }

    /// <summary>
    /// Creates a typed delegate wrapper for a LambdaValue (interpreted) using Expression Trees.
    /// The wrapper: takes typed params → boxes to object?[] → calls InvokeLambda → unboxes result.
    /// </summary>
    private static Delegate CreateInterpretedLambdaWrapper(
        LambdaValue lambda,
        Type delegateType,
        Type[] paramTypes,
        Type returnType)
    {
        // Create typed parameters matching the delegate signature
        var parameters = paramTypes.Select((t, i) => LinqExpression.Parameter(t, $"p{i}")).ToArray();

        // Box parameters into object?[] for lambda invocation
        var argsArray = LinqExpression.NewArrayInit(
            typeof(object),
            parameters.Select(p => LinqExpression.Convert(p, typeof(object))));

        // Get the InvokeLambda method
        var invokeMethod = typeof(MethodInvoker).GetMethod(
            nameof(MethodInvoker.InvokeLambda),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;

        // Call: MethodInvoker.InvokeLambda(lambda, args, context)
        var lambdaConst = LinqExpression.Constant(lambda);
        var contextConst = LinqExpression.Constant(lambda.Closure);
        var invokeCall = LinqExpression.Call(invokeMethod, lambdaConst, argsArray, contextConst);

        // Handle return type (void for Action, typed for Func)
        LinqExpression body;
        if (returnType == typeof(void))
        {
            // Action: ignore return value
            body = LinqExpression.Block(invokeCall, LinqExpression.Empty());
        }
        else
        {
            // Func: convert result to expected return type
            body = LinqExpression.Convert(invokeCall, returnType);
        }

        // Compile the wrapper lambda to a delegate
        var lambdaExpr = LinqExpression.Lambda(delegateType, body, parameters);
        return lambdaExpr.Compile();
    }
}
