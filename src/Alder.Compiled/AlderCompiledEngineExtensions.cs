using System.Linq.Expressions;
using Alder.Compiled.Compilation;
using Alder.Compiled.DynamicLinq;
using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder.Compiled;

/// <summary>
/// Extension methods that expose the compiled backend from <see cref="AlderEngine"/>.
/// These APIs sit above the shared compilation pipeline and package the result either as an
/// <see cref="AlderCompiledExpression{T}"/>, a plain delegate, or a LINQ expression tree.
/// </summary>
public static class AlderCompiledEngineExtensions
{
    private static readonly MethodInfo InvokeTypedCompiledMethod =
        typeof(AlderCompiledEngineExtensions).GetMethod(
            nameof(InvokeTypedCompiled),
            BindingFlags.NonPublic | BindingFlags.Static)!;
    
    /// <summary>
    /// Parses source and immediately attempts compilation.
    /// </summary>
    /// <param name="engine">The engine instance.</param>
    /// <param name="expression">The expression string to parse and compile.</param>
    /// <returns>The parsed expression object (compiled when possible).</returns>
    public static AlderExpression ParseAndCompile(this AlderEngine engine, string expression)
    {
        var access = engine.GetCompiledFeatureAccess();
        access.ThrowIfDisposed();
        var expr = engine.Parse(expression);
        engine.TryCompile(expr);
        return expr;
    }

    /// <summary>
    /// Compiles source and returns an <see cref="AlderCompiledExpression{T}"/> wrapper for repeated invocation.
    /// </summary>
    /// <typeparam name="T">The expected return type of the expression.</typeparam>
    /// <param name="engine">The engine instance.</param>
    /// <param name="expression">The expression string to compile.</param>
    /// <returns>A compiled expression wrapper for repeated invocation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the expression cannot be compiled to IL.</exception>
    public static AlderCompiledExpression<T> Compile<T>(this AlderEngine engine, string expression)
    {
        var access = engine.GetCompiledFeatureAccess();
        access.ThrowIfDisposed();
        var parsed = engine.Parse(expression);
        if (!engine.TryCompile(parsed))
        {
            var reason = access.GetCompilationFailureReason(parsed) ?? "Unknown compilation failure";
            throw new AlderException(
                DiagnosticDescriptors.StrictCompilationFailed,
                $"Cannot compile expression '{expression}': {reason}");
        }

        var compiledInfo = access.GetCompiledInfo(parsed)!;
        var typeVersion = compiledInfo.TypeVersion
            ?? throw new InvalidOperationException("Compiled expression info has no type version. Compilation did not stamp a version.");
        return new AlderCompiledExpression<T>(compiledInfo.Delegate!, engine, access.Config, typeVersion);
    }

    /// <summary>
    /// Compiles source and returns an <see cref="AlderCompiledExpression{T}"/> whose result type is <see cref="object"/>.
    /// </summary>
    /// <param name="engine">The engine instance.</param>
    /// <param name="expression">The expression string to compile.</param>
    /// <returns>A compiled expression wrapper for repeated invocation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the expression cannot be compiled to IL.</exception>
    public static AlderCompiledExpression<object?> Compile(this AlderEngine engine, string expression)
        => Compile<object?>(engine, expression);

    /// <summary>
    /// Creates a reusable Dynamic LINQ factory bound to this engine.
    /// </summary>
    public static IDynamicLambdaFactory CreateDynamicLambdaFactory(this AlderEngine engine)
    {
        engine.GetCompiledFeatureAccess().ThrowIfDisposed();
        return new AlderDynamicLambdaFactory(engine);
    }

    /// <summary>
    /// Compiles source and returns a <see cref="Func{TResult}"/> that closes over the engine state.
    /// Variables assigned on the engine after compilation remain visible to later delegate invocations.
    /// </summary>
    /// <typeparam name="T">The expected return type of the expression.</typeparam>
    /// <param name="engine">The engine instance.</param>
    /// <param name="expression">The expression string to compile.</param>
    /// <returns>A function that evaluates the compiled expression and returns the result.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the expression cannot be compiled to IL.</exception>
    public static Func<T?> CompileToFunc<T>(this AlderEngine engine, string expression)
    {
        engine.GetCompiledFeatureAccess().ThrowIfDisposed();
        var compiled = Compile<T>(engine, expression);
        return () => compiled.Invoke();
    }

    /// <summary>
    /// Compiles a code body with named parameters into a native <typeparamref name="TDelegate"/> delegate.
    /// Parameter types come from the delegate signature rather than from binder inference inside the code body.
    /// </summary>
    /// <typeparam name="TDelegate">A Func or Action delegate type whose generic arguments define parameter types
    /// (and return type for Func). For example, <c>Func&lt;int, string, bool&gt;</c> expects two parameters
    /// (int and string) and returns bool.</typeparam>
    /// <param name="engine">The engine instance. Must have a compiler configured via <c>UseCompiler()</c>.</param>
    /// <param name="code">The code body to compile. Use parameter names directly (not lambda syntax).
    /// For example: <c>"x &gt; 0 ? x * 2 : -x"</c> with parameter name <c>"x"</c>.</param>
    /// <param name="parameterNames">Names for each parameter, matching the delegate's parameter types by position.</param>
    /// <returns>A compiled delegate that can be invoked with typed arguments.</returns>
    /// <exception cref="AlderException">Parameter count doesn't match delegate, or compilation fails.</exception>
    public static TDelegate Compile<TDelegate>(
        this AlderEngine engine,
        string code,
        params string[] parameterNames)
        where TDelegate : Delegate
    {
        return CompileDelegate<TDelegate>(engine, code, parameterNames);
    }

    /// <summary>
    /// Compiles a code body with named parameters into a native <typeparamref name="TDelegate"/> delegate.
    /// This overload accepts an enumerable parameter-name source.
    /// </summary>
    public static TDelegate Compile<TDelegate>(
        this AlderEngine engine,
        string code,
        IEnumerable<string> parameterNames)
        where TDelegate : Delegate
    {
        return CompileDelegate<TDelegate>(engine, code, parameterNames.ToArray());
    }

    private static TDelegate CompileDelegate<TDelegate>(
        AlderEngine engine,
        string code,
        string[] parameterNames)
        where TDelegate : Delegate
    {
        var access = engine.GetCompiledFeatureAccess();
        access.ThrowIfDisposed();

        var delegateType = typeof(TDelegate);
        var invokeMethod = delegateType.GetMethod(nameof(Action.Invoke))
            ?? throw new AlderException(
                DiagnosticDescriptors.ParseAsExpressionRequiresGenericDelegate,
                delegateType.Name);
        var delegateParameters = invokeMethod.GetParameters();
        var paramTypes = delegateParameters.Select(static p => p.ParameterType).ToArray();
        var returnType = invokeMethod.ReturnType;
        var isAction = returnType == typeof(void);

        if (parameterNames.Length != paramTypes.Length)
            throw new AlderException(
                DiagnosticDescriptors.CompileParameterCountMismatch,
                delegateType.Name, paramTypes.Length.ToString(), parameterNames.Length.ToString());

        var additionalVariableTypes = new Dictionary<string, Type>(access.Config.Comparer);
        for (var i = 0; i < parameterNames.Length; i++)
            additionalVariableTypes[parameterNames[i]] = paramTypes[i];

        var parsed = engine.Parse(code);
        var compiledInfo = access.CompileWithAdditionalVariables(parsed, additionalVariableTypes);

        if (compiledInfo.Delegate == null)
        {
            if (compiledInfo.FailureException is AlderException alderFailure)
                throw alderFailure;

            var reason = compiledInfo.FailureReason ?? "Unknown compilation failure";
            throw new AlderException(
                DiagnosticDescriptors.StrictCompilationFailed,
                $"Cannot compile expression '{code}': {reason}");
        }

        var parentTypeVersion = access.GetOrCreateContext().GetTypeInferenceVersion();
        return BuildBridgeDelegate<TDelegate>(
            compiledInfo.Delegate,
            engine,
            access.Config,
            parentTypeVersion,
            parameterNames,
            paramTypes,
            returnType,
            isAction);
    }

    private static TDelegate BuildBridgeDelegate<TDelegate>(
        CompiledExpressionDelegate compiledDelegate,
        AlderEngine engine,
        AlderConfig config,
        int parentTypeVersion,
        string[] paramNames,
        Type[] paramTypes,
        Type returnType,
        bool isAction)
        where TDelegate : Delegate
    {
        var parameters = new ParameterExpression[paramTypes.Length];
        for (int i = 0; i < paramTypes.Length; i++)
            parameters[i] = LinqExpression.Parameter(paramTypes[i], paramNames[i]);

        var compiledDelegateConst = LinqExpression.Constant(compiledDelegate);
        var engineConst = LinqExpression.Constant(engine);
        var configConst = LinqExpression.Constant(config);
        var parentTypeVersionConst = LinqExpression.Constant(parentTypeVersion);
        var argsArray = LinqExpression.NewArrayInit(
            typeof(object),
            parameters.Select(static p => LinqExpression.Convert(p, typeof(object))));
        var invokeCall = LinqExpression.Call(
            InvokeTypedCompiledMethod,
            compiledDelegateConst,
            engineConst,
            configConst,
            parentTypeVersionConst,
            LinqExpression.Constant(paramNames),
            LinqExpression.Constant(paramTypes),
            argsArray,
            LinqExpression.Default(typeof(CancellationToken)));

        if (isAction)
        {
            var block = LinqExpression.Block(invokeCall, LinqExpression.Empty());
            return LinqExpression.Lambda<TDelegate>(block, parameters).Compile();
        }

        LinqExpression resultExpr;
        if (returnType == typeof(object))
        {
            resultExpr = invokeCall;
        }
        else if (returnType.IsValueType)
        {
            // Unbox from the object-returning compiled delegate. The expression tree preserves nullable value semantics.
            resultExpr = LinqExpression.Unbox(invokeCall, returnType);
        }
        else
        {
            resultExpr = LinqExpression.TypeAs(invokeCall, returnType);
        }

        return LinqExpression.Lambda<TDelegate>(resultExpr, parameters).Compile();
    }

    private static object? InvokeTypedCompiled(
        CompiledExpressionDelegate compiledDelegate,
        AlderEngine engine,
        AlderConfig config,
        int parentTypeVersion,
        string[] parameterNames,
        Type[] parameterTypes,
        object?[] values,
        CancellationToken cancellationToken)
    {
        using var state = engine.CreateCompiledInvocationState(parentTypeVersion, cancellationToken);
        for (var i = 0; i < parameterNames.Length; i++)
            state.ExecutionContext.Define(parameterNames[i], values[i], parameterTypes[i]);

        return compiledDelegate(state.ExecutionContext, config, state.ConstraintState, cancellationToken);
    }

    /// <summary>
    /// Parses a lambda expression string into a typed <see cref="Expression{TDelegate}"/>
    /// suitable for Entity Framework, IQueryable providers, and in-memory compilation.
    /// Always parses in Standard mode regardless of engine LanguageMode.
    /// </summary>
    /// <typeparam name="TDelegate">A Func delegate type (e.g., Func&lt;int, bool&gt;).
    /// Parameter types are inferred from the generic arguments.</typeparam>
    /// <param name="engine">The engine instance.</param>
    /// <param name="expression">A lambda expression string (e.g., "x => x > 5").</param>
    /// <returns>A typed expression tree that can be passed to LINQ providers or compiled.</returns>
    /// <exception cref="AlderException">Thrown when the expression contains unsupported nodes
    /// or has parameter/type mismatches.</exception>
    public static Expression<TDelegate> ParseAsExpression<TDelegate>(this AlderEngine engine, string expression)
        where TDelegate : Delegate
        => ParseAsExpression<TDelegate>(engine, expression, null);

    internal static Expression<TDelegate> ParseAsExpression<TDelegate>(
        AlderEngine engine, string expression, Dictionary<string, object?>? additionalVariables)
        where TDelegate : Delegate
    {
        try
        {
            var delegateType = typeof(TDelegate);
            if (!delegateType.IsGenericType)
                throw new AlderException(
                    DiagnosticDescriptors.ParseAsExpressionRequiresGenericDelegate,
                    delegateType.Name);

            var returnType = delegateType.GetGenericArguments()[^1];
            var prepared = QueryExpressionPreparer.PrepareParseAsExpression(
                engine,
                expression,
                delegateType,
                additionalVariables);
            var body = new QueryTreeExporter(prepared.Parameters, prepared.CapturedVariables)
                .Export(prepared.BoundBody);

            if (body.Type != returnType)
            {
                if (!body.Type.IsValueType && returnType.IsAssignableFrom(body.Type))
                    return LinqExpression.Lambda<TDelegate>(body, prepared.Parameters);

                try
                {
                    body = LinqExpression.Convert(body, returnType);
                }
                catch (InvalidOperationException ex)
                {
                    throw new AlderException(
                        DiagnosticDescriptors.CantConvAnonMethReturnType,
                        ex,
                        delegateType.Name);
                }
            }

            return LinqExpression.Lambda<TDelegate>(body, prepared.Parameters);
        }
        catch (InsufficientExecutionStackException ex)
        {
            throw new AlderException(DiagnosticDescriptors.ExpressionNestingDepthExceeded, ex);
        }
    }

    /// <summary>
    /// Attempts to parse a lambda expression string into a typed expression tree.
    /// Returns false with diagnostics if parsing or translation fails.
    /// </summary>
    /// <typeparam name="TDelegate">A Func delegate type.</typeparam>
    /// <param name="engine">The engine instance.</param>
    /// <param name="expression">A lambda expression string.</param>
    /// <param name="result">The resulting expression tree, or null if parsing failed.</param>
    /// <param name="diagnostics">Diagnostic information when parsing fails.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    public static bool TryParseAsExpression<TDelegate>(
        this AlderEngine engine,
        string expression,
        out Expression<TDelegate>? result,
        out IReadOnlyList<AlderDiagnostic> diagnostics)
        where TDelegate : Delegate
    {
        engine.GetCompiledFeatureAccess().ThrowIfDisposed();
        try
        {
            result = ParseAsExpression<TDelegate>(engine, expression);
            diagnostics = [];
            return true;
        }
        catch (Exception ex)
        {
            result = null;
            diagnostics = [AlderDiagnostic.FromException(ex)];
            return false;
        }
    }

    /// <summary>
    /// Parses a lambda expression string into an expression tree and compiles it to a native delegate.
    /// Equivalent to <c>ParseAsExpression&lt;TDelegate&gt;(expression).Compile()</c>.
    /// </summary>
    public static TDelegate CompileExpression<TDelegate>(this AlderEngine engine, string expression)
        where TDelegate : Delegate
        => CompileExpression<TDelegate>(engine, expression, null);

    internal static TDelegate CompileExpression<TDelegate>(
        AlderEngine engine, string expression, Dictionary<string, object?>? additionalVariables)
        where TDelegate : Delegate
    {
        engine.GetCompiledFeatureAccess().ThrowIfDisposed();
        return ParseAsExpression<TDelegate>(engine, expression, additionalVariables).Compile();
    }
}
