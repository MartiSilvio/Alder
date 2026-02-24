using System.Collections.Concurrent;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compilation;

/// <summary>
/// Thread-safe cache for compiled expression delegates.
/// Instance-based caching per engine - cache is shared with child engines and cleaned up when root engine is disposed.
/// </summary>
internal sealed class ExpressionCache
{
    private readonly ConcurrentDictionary<string, CompiledExpressionInfo> _cache = new();

    public CompiledExpressionInfo GetOrAdd(string key, Func<string, CompiledExpressionInfo> valueFactory)
    {
        return _cache.GetOrAdd(key, valueFactory);
    }

    public bool TryGetValue(string key, out CompiledExpressionInfo value)
    {
        return _cache.TryGetValue(key, out value!);
    }

    public void Clear()
    {
        _cache.Clear();
    }
}

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
    {
        try
        {
            var context = new CsEvalContext(CsEvalConfig.Empty);
            var opts = options ?? CsEvalOptions.Default;
            var (ilDelegate, failureReason) = CompilerContext.TryCompile(ast, context, opts);

            if (ilDelegate != null)
            {
                return new CompiledExpressionInfo(Compiled, true, null);

                object? Compiled(CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct,
                    Func<MethodInfo, object?[], object?[]>? argumentTransformer)
                    => ilDelegate(ctx, opts, ct, argumentTransformer);
            }

            return new CompiledExpressionInfo(null, false, failureReason);
        }
        catch (CsEvalDepthException)
        {
            throw; // Depth limits are recoverable — let them propagate so callers can surface them
        }
        catch (Exception ex)
        {
            return new CompiledExpressionInfo(null, false, ex.Message);
        }
    }
}
