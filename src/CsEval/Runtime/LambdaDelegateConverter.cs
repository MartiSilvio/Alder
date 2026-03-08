using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using CsEval.Compilation;
using CsEval.Diagnostics;
using CsEval.Interpretation;
using CsEval.Parsing;

namespace CsEval.Runtime;

/// <summary>
/// Converts CsEval lambda values (LambdaValue, CompiledLambdaValue) to System.Func/Action delegates.
/// </summary>
internal static class LambdaDelegateConverter
{
    private static readonly HashSet<Type> SupportedFuncDefinitions = CreateOpenGenericDelegateSet("Func", 1, 17);
    private static readonly HashSet<Type> SupportedActionDefinitions = CreateOpenGenericDelegateSet("Action", 1, 16);

    private static readonly MethodInfo InvokeLambdaMethod = typeof(MethodInvoker).GetMethod(
        nameof(MethodInvoker.InvokeLambda),
        BindingFlags.Static | BindingFlags.NonPublic)!;

    // Cache compiled delegate wrappers by (lambda identity, delegate type signature)
    // Key: lambda hashcode + delegate type, Value: compiled delegate
    private static readonly ConcurrentDictionary<(int, Type), Delegate> _delegateCache = new();
    private static readonly ConditionalWeakTable<LambdaExpr, ConcurrentDictionary<Type, Delegate>> _typedDelegateCacheBySource = new();
    private static readonly ConditionalWeakTable<LambdaExpr, StrongBox<bool>> _typedEligibilityBySource = new();

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

        var delegateDefinition = type.GetGenericTypeDefinition();
        return SupportedFuncDefinitions.Contains(delegateDefinition) ||
               SupportedActionDefinitions.Contains(delegateDefinition);
    }

    /// <summary>
    /// Converts a CompiledLambdaValue to a typed delegate.
    /// Uses caching to avoid recompiling the same wrapper multiple times.
    /// </summary>
    private static Delegate ConvertCompiledLambda(CompiledLambdaValue lambda, Type delegateType)
    {
        var (paramTypes, returnType) = GetDelegateSignature(delegateType);
        ValidateSignature(lambda.Parameters.Count, paramTypes.Length, delegateType);

        if (lambda.Source is { } source && IsTypedDelegateEligible(source))
        {
            var perSourceCache = _typedDelegateCacheBySource.GetOrCreateValue(source);
            if (perSourceCache.TryGetValue(delegateType, out var cachedTyped))
                return cachedTyped;

            var typedDelegate = TryCompileTypedDelegate(lambda, delegateType, paramTypes, returnType);
            if (typedDelegate != null)
                return perSourceCache.GetOrAdd(delegateType, typedDelegate);
        }

        var cacheKey = (System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(lambda), delegateType);
        return _delegateCache.GetOrAdd(
            cacheKey,
            _ => CreateCompiledLambdaWrapper(lambda, delegateType, paramTypes, returnType));
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

        // Call the arity-specialized runtime invoker to avoid per-call object[] allocations.
        var lambdaConst = LinqExpression.Constant(lambda);
        var invokeCall = CreateCompiledInvokeCall(lambdaConst, parameters);

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

    private static Delegate? TryCompileTypedDelegate(
        CompiledLambdaValue lambda,
        Type delegateType,
        Type[] paramTypes,
        Type returnType)
    {
        if (lambda.Source == null || !CanCompileTyped(lambda.Source))
            return null;

        try
        {
            var parameterScope = new Dictionary<string, System.Linq.Expressions.ParameterExpression>(StringComparer.Ordinal);
            var parameters = new System.Linq.Expressions.ParameterExpression[paramTypes.Length];
            for (var i = 0; i < paramTypes.Length; i++)
            {
                var paramName = lambda.Source.Parameters[i].Name.Lexeme;
                var paramExpr = LinqExpression.Parameter(paramTypes[i], paramName);
                parameterScope[paramName] = paramExpr;
                parameters[i] = paramExpr;
            }

            // Only safe for parameter-only lambdas; no closure variable snapshots.
            var emitter = new ExpressionTreeEmitter(parameterScope, new Dictionary<string, object?>(StringComparer.Ordinal), lambda.Closure.TypeResolver);
            LinqExpression body = emitter.Emit(lambda.Source.Body);

            if (returnType == typeof(void))
                body = LinqExpression.Block(body, LinqExpression.Empty());
            else if (body.Type != returnType)
                body = LinqExpression.Convert(body, returnType);

            return LinqExpression.Lambda(delegateType, body, parameters).Compile();
        }
        catch (CsEvalException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
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

    private static bool IsTypedDelegateEligible(LambdaExpr lambda) =>
        _typedEligibilityBySource.GetValue(
            lambda,
            static l => new StrongBox<bool>(CanCompileTyped(l))).Value;

    private static bool CanCompileTyped(LambdaExpr lambda)
    {
        var collector = new VariableCollector();
        collector.Collect(lambda);
        return collector.Variables.Count == 0;
    }

    private static LinqExpression CreateCompiledInvokeCall(
        LinqExpression lambdaConst,
        IReadOnlyList<System.Linq.Expressions.ParameterExpression> parameters)
    {
        return parameters.Count switch
        {
            0 => LinqExpression.Call(GetMethodInvokerMethod(nameof(MethodInvoker.InvokeCompiledLambda0)), lambdaConst),
            1 => LinqExpression.Call(
                GetMethodInvokerMethod(nameof(MethodInvoker.InvokeCompiledLambda1), typeof(object)),
                lambdaConst,
                LinqExpression.Convert(parameters[0], typeof(object))),
            2 => LinqExpression.Call(
                GetMethodInvokerMethod(nameof(MethodInvoker.InvokeCompiledLambda2), typeof(object), typeof(object)),
                lambdaConst,
                LinqExpression.Convert(parameters[0], typeof(object)),
                LinqExpression.Convert(parameters[1], typeof(object))),
            _ => LinqExpression.Call(
                GetMethodInvokerMethod(nameof(MethodInvoker.InvokeCompiledLambda), typeof(object[])),
                lambdaConst,
                LinqExpression.NewArrayInit(
                    typeof(object),
                    parameters.Select(p => LinqExpression.Convert(p, typeof(object)))))
        };
    }

    private static MethodInfo GetMethodInvokerMethod(string name, params Type[] argumentTypes)
    {
        var method = typeof(MethodInvoker).GetMethod(
            name,
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            types: [typeof(CompiledLambdaValue), .. argumentTypes],
            modifiers: null);

        return method ?? throw new InvalidOperationException($"MethodInvoker.{name} not found");
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

        // Call: MethodInvoker.InvokeLambda(lambda, args, context)
        var lambdaConst = LinqExpression.Constant(lambda);
        var contextConst = LinqExpression.Constant(lambda.Closure);
        var invokeCall = LinqExpression.Call(InvokeLambdaMethod, lambdaConst, argsArray, contextConst);

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
