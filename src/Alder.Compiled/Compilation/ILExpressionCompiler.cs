using Alder.Compilation;
using Alder.Binding;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Compiled.Compilation;

/// <summary>
/// Orchestrates bound-node IL compilation.
/// </summary>
internal static class ILExpressionCompiler
{
    /// <summary>
    /// Get or create compiled delegate for an expression string.
    /// </summary>
    public static CompiledExpressionInfo GetOrCompile(string expressionText, Expr ast, ExpressionCache cache, AlderConfig config)
    {
        return cache.GetOrAdd(expressionText, _ => TryCompile(ast, config));
    }

    /// <summary>
    /// Attempt to compile an AST to a native IL delegate.
    /// </summary>
    public static CompiledExpressionInfo TryCompile(Expr ast, AlderConfig config)
    {
        var compilerName = config.ExpressionCompiler.GetType().Name;
        try
        {
            var context = new AlderContext(AlderConfig.Empty);
            var binder = new Binding.Binder();
            var bound = binder.Bind(ast, new BindingContext(context));

            var compiled = TryCompileBound(bound, config);
            if (compiled != null)
            {
                return new CompiledExpressionInfo(
                    Delegate: compiled,
                    IsCompilable: true,
                    FailureReason: null,
                    Pipeline: CompiledPipeline.Bound);
            }

            return new CompiledExpressionInfo(null, false, $"{compilerName}: Bound compilation returned null");
        }
        catch (Exception ex) when (ex is not InsufficientExecutionStackException)
        {
            return new CompiledExpressionInfo(null, false, $"{compilerName}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Attempt to compile a pre-bound expression to a native IL delegate.
    /// </summary>
    public static CompiledExpressionInfo TryCompile(BoundExpr bound, AlderConfig? config = null)
    {
        var cfg = config ?? AlderConfig.Empty;
        var compilerName = cfg.ExpressionCompiler.GetType().Name;
        try
        {
            var compiled = TryCompileBound(bound, cfg);
            if (compiled != null)
            {
                return new CompiledExpressionInfo(
                    Delegate: compiled,
                    IsCompilable: true,
                    FailureReason: null,
                    Pipeline: CompiledPipeline.Bound);
            }

            return new CompiledExpressionInfo(null, false, $"{compilerName}: Bound compilation returned null");
        }
        catch (Exception ex) when (ex is not InsufficientExecutionStackException)
        {
            return new CompiledExpressionInfo(null, false, $"{compilerName}: {ex.Message}", ex);
        }
    }

    private static CompiledExpressionDelegate? TryCompileBound(BoundExpr bound, AlderConfig config)
    {
        try
        {
            var contextParam = LinqExpression.Parameter(typeof(AlderContext), "context");
            var configParam = LinqExpression.Parameter(typeof(AlderConfig), "config");
            var constraintStateParam = LinqExpression.Parameter(typeof(ExecutionConstraintState), "constraintState");
            var ctParam = LinqExpression.Parameter(typeof(CancellationToken), "ct");

            var emitter = new BoundExpressionEmitter(
                contextParam,
                configParam,
                constraintStateParam,
                ctParam,
                config.PreferResolvedRuntimeDispatch);
            var body = emitter.EmitRoot(bound);
            if (body.Type != typeof(object))
                body = LinqExpression.Convert(body, typeof(object));

            var lambda = LinqExpression.Lambda<CompiledExpressionDelegate>(body, contextParam, configParam, constraintStateParam, ctParam);
            return config.ExpressionCompiler.Compile(lambda);
        }
        catch (BindingNotSupportedException)
        {
            return null;
        }
    }
}
