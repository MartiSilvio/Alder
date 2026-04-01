using System.Linq.Expressions;
using Alder.Binding;
using Alder.Compiled.Compilation;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Compiled;

public static class AlderCompiledEngineExtensions
{
    /// <summary>
    /// Parses and attempts to compile an expression in one step.
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
    /// Compiles an expression and returns a <see cref="AlderCompiledExpression{T}"/> that can be invoked
    /// repeatedly without engine dispatch overhead. Throws if the expression cannot be compiled.
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
            var reason = parsed.CompilationFailureReason ?? "Unknown compilation failure";
            throw new AlderException(
                DiagnosticDescriptors.StrictCompilationFailed,
                $"Cannot compile expression '{expression}': {reason}");
        }

        var compiledInfo = parsed.GetCompiledInfo()!;
        var typeVersion = compiledInfo.TypeVersion
            ?? throw new InvalidOperationException("Compiled expression info has no type version — compilation did not stamp a version.");
        return new AlderCompiledExpression<T>(compiledInfo.Delegate!, engine, access.Config, typeVersion);
    }

    /// <summary>
    /// Compiles an expression and returns a <see cref="AlderCompiledExpression{T}"/> with object? result type.
    /// Throws if the expression cannot be compiled.
    /// </summary>
    /// <param name="engine">The engine instance.</param>
    /// <param name="expression">The expression string to compile.</param>
    /// <returns>A compiled expression wrapper for repeated invocation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the expression cannot be compiled to IL.</exception>
    public static AlderCompiledExpression<object?> Compile(this AlderEngine engine, string expression)
        => Compile<object?>(engine, expression);

    /// <summary>
    /// Compiles an expression and returns a <see cref="Func{T}"/> for zero-overhead hot-path invocation.
    /// The returned delegate captures the engine context by reference -- variables set via
    /// <see cref="AlderEngine.SetVariable"/> after compilation are visible to subsequent invocations.
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
    {
        var access = engine.GetCompiledFeatureAccess();
        access.ThrowIfDisposed();
        try
        {
            var delegateType = typeof(TDelegate);
            if (!delegateType.IsGenericType)
                throw new AlderException(
                    DiagnosticDescriptors.ParseAsExpressionRequiresGenericDelegate,
                    delegateType.Name);

            var genericArgs = delegateType.GetGenericArguments();
            var paramTypes = genericArgs[..^1];
            var returnType = genericArgs[^1];

            var lexer = new Lexer(expression);
            var tokens = lexer.Tokenize();
            var parser = ExpressionParser.CreateForSubExpression(tokens, LanguageMode.Standard);
            var ast = parser.Parse();

            if (ast is not LambdaExpr lambdaExpr)
                throw new AlderException(
                    DiagnosticDescriptors.ParseAsExpressionRequiresLambda,
                    "x => x > 5");

            if (lambdaExpr.Parameters.Count != paramTypes.Length)
                throw new AlderException(
                    DiagnosticDescriptors.CantConvAnonMethParams,
                    delegateType.Name);

            var parameterScope = new Dictionary<string, ParameterExpression>();
            var parameterExpressions = new ParameterExpression[paramTypes.Length];
            for (var i = 0; i < paramTypes.Length; i++)
            {
                var paramName = lambdaExpr.Parameters[i].Name.Lexeme;
                var paramExpr = LinqExpression.Parameter(paramTypes[i], paramName);
                parameterScope[paramName] = paramExpr;
                parameterExpressions[i] = paramExpr;
            }

            var engineVariables = access.CollectEngineVariables();
            var config = access.Config;

            var bindingRuntimeContext = new AlderContext(config);
            foreach (var pair in engineVariables)
                bindingRuntimeContext.Define(pair.Key, pair.Value, pair.Value?.GetType() ?? typeof(object));

            for (var i = 0; i < paramTypes.Length; i++)
            {
                var parameterName = lambdaExpr.Parameters[i].Name.Lexeme;
                bindingRuntimeContext.Define(parameterName, null, paramTypes[i]);
            }

            var binder = new Alder.Binding.Binder();
            var boundBody = binder.Bind(lambdaExpr.Body, new BindingContext(bindingRuntimeContext));

            var emitter = new ExpressionTreeEmitter(parameterScope, engineVariables, config.TypeResolver);
            var body = emitter.Emit(boundBody);

            if (body.Type != returnType)
            {
                try
                {
                    body = LinqExpression.Convert(body, returnType);
                }
                catch (InvalidOperationException)
                {
                    throw new AlderException(
                        DiagnosticDescriptors.CantConvAnonMethReturnType,
                        delegateType.Name);
                }
            }

            return LinqExpression.Lambda<TDelegate>(body, parameterExpressions);
        }
        catch (InsufficientExecutionStackException)
        {
            throw new AlderException(DiagnosticDescriptors.ExpressionNestingDepthExceeded);
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
    {
        engine.GetCompiledFeatureAccess().ThrowIfDisposed();
        return ParseAsExpression<TDelegate>(engine, expression).Compile();
    }
}
