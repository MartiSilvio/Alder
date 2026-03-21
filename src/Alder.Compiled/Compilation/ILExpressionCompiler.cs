using Alder.Compilation;
using Alder.Binding;
using Alder.Binding.Optimization;
using Alder.Parsing;
using Alder.Runtime;
using System.Linq.Expressions;

namespace Alder.Compiled.Compilation;

/// <summary>
/// Orchestrates bound-node IL compilation.
/// </summary>
internal static class ILExpressionCompiler
{
    private readonly record struct CompiledBoundDelegates(
        CompiledExpressionDelegate Standard,
        CompiledExpressionFastDelegate? Fast);

    /// <summary>
    /// Get or create compiled delegate for an expression string.
    /// </summary>
    public static CompiledExpressionInfo GetOrCompile(string expressionText, Expr ast, ExpressionCache cache, AlderOptions? options = null)
    {
        return cache.GetOrAdd(expressionText, _ => TryCompile(ast, options));
    }

    /// <summary>
    /// Attempt to compile an AST to a native IL delegate.
    /// </summary>
    public static CompiledExpressionInfo TryCompile(Expr ast, AlderOptions? options = null)
    {
        var opts = options ?? AlderOptions.Default;
        var compilerName = opts.ExpressionCompiler.GetType().Name;
        try
        {
            var context = new AlderContext(AlderConfig.Empty);
            AstDepthValidator.EnsureWithinLimit(ast, opts.MaxExpressionDepth);
            var binder = new Alder.Binding.Binder();
            var bound = binder.Bind(ast, new BindingContext(context));

            var compiledDelegates = TryCompileBound(bound, opts);
            if (compiledDelegates != null)
            {
                return new CompiledExpressionInfo(
                    Delegate: compiledDelegates.Value.Standard,
                    IsCompilable: true,
                    FailureReason: null,
                    Pipeline: CompiledPipeline.Bound,
                    FastDelegate: compiledDelegates.Value.Fast,
                    FastDelegateOptions: compiledDelegates.Value.Fast != null ? opts : null);
            }

            return new CompiledExpressionInfo(null, false, $"{compilerName}: Bound compilation returned null");
        }
        catch (AlderDepthException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new CompiledExpressionInfo(null, false, $"{compilerName}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Attempt to compile a pre-bound expression to a native IL delegate.
    /// </summary>
    public static CompiledExpressionInfo TryCompile(BoundExpr bound, AlderOptions? options = null)
    {
        var opts = options ?? AlderOptions.Default;
        var compilerName = opts.ExpressionCompiler.GetType().Name;
        try
        {
            var compiledDelegates = TryCompileBound(bound, opts);
            if (compiledDelegates != null)
            {
                return new CompiledExpressionInfo(
                    Delegate: compiledDelegates.Value.Standard,
                    IsCompilable: true,
                    FailureReason: null,
                    Pipeline: CompiledPipeline.Bound,
                    FastDelegate: compiledDelegates.Value.Fast,
                    FastDelegateOptions: compiledDelegates.Value.Fast != null ? opts : null);
            }

            return new CompiledExpressionInfo(null, false, $"{compilerName}: Bound compilation returned null");
        }
        catch (AlderDepthException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new CompiledExpressionInfo(null, false, $"{compilerName}: {ex.Message}", ex);
        }
    }

    private static CompiledBoundDelegates? TryCompileBound(BoundExpr bound, AlderOptions options)
    {
        try
        {
            var contextParam = LinqExpression.Parameter(typeof(AlderContext), "context");
            var optionsParam = LinqExpression.Parameter(typeof(AlderOptions), "options");
            var ctParam = LinqExpression.Parameter(typeof(CancellationToken), "ct");

            var optimized = BoundTreeOptimizer.Optimize(bound);

            var emitter = new BoundExpressionEmitter(contextParam, optionsParam, ctParam);
            var body = emitter.EmitRoot(optimized);
            if (body.Type != typeof(object))
                body = LinqExpression.Convert(body, typeof(object));

            var lambda = LinqExpression.Lambda<CompiledExpressionDelegate>(body, contextParam, optionsParam, ctParam);
            var standardDelegate = options.ExpressionCompiler.Compile(lambda);

            var rewrittenBody = new ParameterSubstitutionVisitor(
                optionsParam,
                ctParam,
                LinqExpression.Constant(options, typeof(AlderOptions)),
                LinqExpression.Constant(default(CancellationToken), typeof(CancellationToken)))
                .Visit(body)!;

            var fastLambda = LinqExpression.Lambda<CompiledExpressionFastDelegate>(rewrittenBody, contextParam);
            var fastDelegate = options.ExpressionCompiler.Compile(fastLambda);

            return new CompiledBoundDelegates(standardDelegate, fastDelegate);
        }
        catch (BindingNotSupportedException ex) when (IsDepthFailure(ex.Message))
        {
            throw new AlderDepthException("binding", options.MaxExpressionDepth);
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

    private sealed class ParameterSubstitutionVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _optionsParam;
        private readonly ParameterExpression _ctParam;
        private readonly LinqExpression _optionsReplacement;
        private readonly LinqExpression _ctReplacement;

        public ParameterSubstitutionVisitor(
            ParameterExpression optionsParam,
            ParameterExpression ctParam,
            LinqExpression optionsReplacement,
            LinqExpression ctReplacement)
        {
            _optionsParam = optionsParam;
            _ctParam = ctParam;
            _optionsReplacement = optionsReplacement;
            _ctReplacement = ctReplacement;
        }

        protected override LinqExpression VisitParameter(ParameterExpression node)
        {
            if (node == _optionsParam)
                return _optionsReplacement;
            if (node == _ctParam)
                return _ctReplacement;
            return base.VisitParameter(node);
        }
    }
}
