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
/// Default expression compiler using System.Linq.Expressions.
/// </summary>
internal static class ExpressionCompiler
{
    /// <summary>
    /// Get or create compiled delegate for an expression string.
    /// </summary>
    public static CompiledExpressionInfo GetOrCompile(string expressionText, Expr ast, ExpressionCache cache)
    {
        return cache.GetOrAdd(expressionText, _ => TryCompile(ast));
    }

    /// <summary>
    /// Attempt to compile an AST to a native IL delegate.
    /// </summary>
    public static CompiledExpressionInfo TryCompile(Expr ast)
    {
        try
        {
            var context = new CsEvalContext(CsEvalConfig.Empty);
            var options = CsEvalOptions.Default;
            var (ilDelegate, failureReason) = CompilerContext.TryCompile(ast, context, options);

            if (ilDelegate != null)
            {
                return new CompiledExpressionInfo(Compiled, true, null);

                object? Compiled(CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct,
                    Func<MethodInfo, object?[], object?[]>? argumentTransformer)
                    => ilDelegate(ctx, opts, ct, argumentTransformer);
            }

            return new CompiledExpressionInfo(null, false, failureReason);
        }
        catch (Exception ex)
        {
            return new CompiledExpressionInfo(null, false, ex.Message);
        }
    }
}
