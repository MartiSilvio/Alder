using CsEval.Compilation;
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
            var (ilDelegate, failureReason, failureException) = CompilerContext.TryCompile(ast, context, opts);

            if (ilDelegate != null)
            {
                return new CompiledExpressionInfo(Compiled, true, null);

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
}
