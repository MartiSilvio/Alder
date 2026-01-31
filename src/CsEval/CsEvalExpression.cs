using System.Reflection;
using CsEval.Evaluation;
using CsEval.Parsing;

namespace CsEval;

/// <summary>
/// Represents a pre-parsed expression that can be evaluated multiple times
/// with different variable values without re-parsing.
/// </summary>
public sealed class CsEvalExpression
{
    internal Expr Ast { get; }

    /// <summary>
    /// The original expression string.
    /// </summary>
    public string Expression { get; }

    // Compilation state (volatile for thread-safe reads)
    private volatile CompiledExpressionInfo? _compiledInfo;
    private readonly ExpressionCache? _expressionCache;

    internal CsEvalExpression(string expression, Expr ast) : this(expression, ast, null)
    {
    }

    internal CsEvalExpression(string expression, Expr ast, ExpressionCache? expressionCache)
    {
        Expression = expression;
        Ast = ast;
        _expressionCache = expressionCache;
    }

    /// <summary>
    /// Returns true if this expression has been successfully compiled.
    /// </summary>
    public bool IsCompiled => _compiledInfo?.Delegate != null;

    /// <summary>
    /// Returns true if this expression can be compiled.
    /// Returns null if compilation has not been attempted.
    /// </summary>
    public bool? IsCompilable => _compiledInfo?.IsCompilable;

    /// <summary>
    /// Returns the reason why compilation failed, or null if compilation succeeded or hasn't been attempted.
    /// </summary>
    public string? CompilationFailureReason => _compiledInfo?.FailureReason;

    /// <summary>
    /// Attempts to compile this expression. Returns true if successful.
    /// </summary>
    public bool TryCompile()
    {
        if (_compiledInfo != null)
            return _compiledInfo.Delegate != null;

        var info = _expressionCache != null ? 
            ExpressionCompiler.GetOrCompile(Expression, Ast, _expressionCache) : 
            ExpressionCompiler.TryCompile(Ast);

        _compiledInfo = info;
        return info.Delegate != null;
    }

    /// <summary>
    /// Compiles this expression. Throws if compilation fails.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when compilation fails.</exception>
    public void Compile()
    {
        if (!TryCompile())
        {
            throw new InvalidOperationException(
                $"Cannot compile expression '{Expression}': {_compiledInfo?.FailureReason ?? "Unknown reason"}");
        }
    }

    internal CompiledExpressionInfo? GetCompiledInfo() => _compiledInfo;

    internal void SetCompiledInfo(CompiledExpressionInfo info) => _compiledInfo = info;
}

/// <summary>
/// Delegate type for compiled expressions.
/// </summary>
/// <param name="context">The evaluation context containing variables.</param>
/// <param name="options">The evaluation options.</param>
/// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
/// <param name="functions">Registered functions dictionary.</param>
/// <param name="argumentTransformer">Optional argument transformer for method calls.</param>
/// <returns>The evaluated result.</returns>
public delegate object? CompiledExpression(
    CsEvalContext context,
    CsEvalOptions options,
    CancellationToken cancellationToken,
    Dictionary<string, Func<object?[], object?>> functions,
    Func<MethodInfo, object?[], object?[]>? argumentTransformer);

/// <summary>
/// Contains information about a compiled expression.
/// </summary>
/// <param name="Delegate">The compiled delegate, or null if compilation failed.</param>
/// <param name="IsCompilable">Whether the expression can be compiled.</param>
/// <param name="FailureReason">The reason compilation failed, or null if it succeeded.</param>
internal record CompiledExpressionInfo(
    CompiledExpression? Delegate,
    bool IsCompilable,
    string? FailureReason);
