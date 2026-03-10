using CsEval.Compilation;
using CsEval.Binding;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compiled.Compilation;

/// <summary>
/// Orchestrates AST-to-IL compilation via <see cref="CompilerContext"/>.
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
        => TryCompile(ast, options, null);

    public static CompiledExpressionInfo TryCompile(Expr ast, CsEvalOptions? options, CsEvalContext? typeHintContext)
    {
        try
        {
            var context = typeHintContext ?? new CsEvalContext(CsEvalConfig.Empty);
            var opts = options ?? CsEvalOptions.Default;

            var boundDelegate = TryCompileBound(ast, context, opts);
            if (boundDelegate != null)
            {
                return new CompiledExpressionInfo(CompiledBound, true, null, Pipeline: CompiledPipeline.Bound);

                object? CompiledBound(CsEvalContext ctx, CsEvalOptions optionsValue, CancellationToken ct)
                    => boundDelegate(ctx, optionsValue, ct);
            }

            var (ilDelegate, failureReason, failureException) = CompilerContext.TryCompile(ast, context, opts);

            if (ilDelegate != null)
            {
                return new CompiledExpressionInfo(Compiled, true, null, Pipeline: CompiledPipeline.Ast);

                object? Compiled(CsEvalContext ctx, CsEvalOptions optionsValue, CancellationToken ct)
                    => ilDelegate(ctx, optionsValue, ct);
            }

            return new CompiledExpressionInfo(null, false, failureReason, failureException);
        }
        catch (CsEvalDepthException)
        {
            throw; // Depth limits are recoverable — let them propagate so callers can surface them
        }
        catch (Exception ex)
        {
            return new CompiledExpressionInfo(null, false, ex.Message, ex);
        }
    }

    private static ILCompiledDelegate? TryCompileBound(Expr ast, CsEvalContext context, CsEvalOptions options)
    {
        try
        {
            var binder = new CsEval.Binding.Binder();
            var bound = binder.Bind(ast, new BindingContext(context));

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
        catch (BindingNotSupportedException)
        {
            return null;
        }
    }
}
