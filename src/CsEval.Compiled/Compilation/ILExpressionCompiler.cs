using CsEval.Compilation;
using CsEval.Binding;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compiled.Compilation;

internal delegate object? ILCompiledDelegate(
    CsEvalContext context,
    CsEvalOptions options,
    CancellationToken ct);

/// <summary>
/// Orchestrates bound-node IL compilation.
/// </summary>
internal static class ILExpressionCompiler
{
    /// <summary>
    /// Get or create compiled delegate for an expression string.
    /// </summary>
    public static CompiledExpressionInfo GetOrCompile(string expressionText, Expr ast, ExpressionCache cache, CsEvalOptions? options = null)
    {
        return cache.GetOrAdd(expressionText, _ => TryCompile(ast, options));
    }

    /// <summary>
    /// Attempt to compile an AST to a native IL delegate.
    /// </summary>
    public static CompiledExpressionInfo TryCompile(Expr ast, CsEvalOptions? options = null)
    {
        try
        {
            var context = new CsEvalContext(CsEvalConfig.Empty);
            var opts = options ?? CsEvalOptions.Default;
            AstDepthValidator.EnsureWithinLimit(ast, opts.MaxExpressionDepth);
            var binder = new CsEval.Binding.Binder();
            var bound = binder.Bind(ast, new BindingContext(context));

            var boundDelegate = TryCompileBound(bound, opts);
            if (boundDelegate != null)
            {
                return new CompiledExpressionInfo(CompiledBound, true, null, Pipeline: CompiledPipeline.Bound);

                object? CompiledBound(CsEvalContext ctx, CsEvalOptions optionsValue, CancellationToken ct)
                    => boundDelegate(ctx, optionsValue, ct);
            }

            return new CompiledExpressionInfo(null, false, "Bound compilation returned null");
        }
        catch (CsEvalDepthException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new CompiledExpressionInfo(null, false, ex.Message, ex);
        }
    }

    /// <summary>
    /// Attempt to compile a pre-bound expression to a native IL delegate.
    /// </summary>
    public static CompiledExpressionInfo TryCompile(BoundExpr bound, CsEvalOptions? options = null)
    {
        try
        {
            var opts = options ?? CsEvalOptions.Default;
            var boundDelegate = TryCompileBound(bound, opts);
            if (boundDelegate != null)
            {
                return new CompiledExpressionInfo(CompiledBound, true, null, Pipeline: CompiledPipeline.Bound);

                object? CompiledBound(CsEvalContext ctx, CsEvalOptions optionsValue, CancellationToken ct)
                    => boundDelegate(ctx, optionsValue, ct);
            }

            return new CompiledExpressionInfo(null, false, "Bound compilation returned null");
        }
        catch (CsEvalDepthException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new CompiledExpressionInfo(null, false, ex.Message, ex);
        }
    }

    private static ILCompiledDelegate? TryCompileBound(BoundExpr bound, CsEvalOptions options)
    {
        try
        {
            var contextParam = LinqExpression.Parameter(typeof(CsEvalContext), "context");
            var optionsParam = LinqExpression.Parameter(typeof(CsEvalOptions), "options");
            var ctParam = LinqExpression.Parameter(typeof(CancellationToken), "ct");

            var emitter = new BoundExpressionEmitter(contextParam, optionsParam, ctParam);
            var body = emitter.Emit(bound);
            if (body.Type != typeof(object))
                body = LinqExpression.Convert(body, typeof(object));

            var lambda = LinqExpression.Lambda<ILCompiledDelegate>(body, contextParam, optionsParam, ctParam);
            return options.ExpressionCompiler.Compile(lambda);
        }
        catch (BindingNotSupportedException ex) when (IsDepthFailure(ex.Message))
        {
            throw new CsEvalDepthException("binding", options.MaxExpressionDepth);
        }
        catch (BindingNotSupportedException)
        {
            return null;
        }
    }

    private static bool IsDepthFailure(string? message)
    {
        return !string.IsNullOrEmpty(message) &&
               message.Contains("nesting depth exceeded", StringComparison.OrdinalIgnoreCase);
    }
}
