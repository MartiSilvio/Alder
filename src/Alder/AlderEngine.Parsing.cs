using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder;

public sealed partial class AlderEngine
{
    /// <summary>
    /// Parses source into a reusable <see cref="AlderExpression"/>.
    /// </summary>
    /// <param name="expression">Expression source to parse.</param>
    /// <returns>A parsed expression ready for evaluation.</returns>
    /// <exception cref="ObjectDisposedException">The engine has been disposed.</exception>
    /// <exception cref="AlderException">The expression contains syntax errors.</exception>
    public AlderExpression Parse(string expression)
    {
        if (expression is null) throw new ArgumentNullException(nameof(expression));
        ThrowIfDisposed();
        try
        {
            var lexer = new Lexer(expression);
            var tokens = lexer.Tokenize();

            var parser = ExpressionParser.CreateForSubExpression(tokens, _config.LanguageMode);
            var ast = parser.Parse();

            return new AlderExpression(expression, ast, _expressionCache);
        }
        catch (InsufficientExecutionStackException)
        {
            throw new AlderException(DiagnosticDescriptors.ExpressionNestingDepthExceeded);
        }
    }

    /// <summary>
    /// Attempts to parse source without throwing for ordinary parse failures.
    /// </summary>
    /// <param name="expression">Expression source to parse.</param>
    /// <param name="result">When successful, the parsed expression; otherwise, <c>null</c>.</param>
    /// <param name="error">When parsing fails, the error message; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public bool TryParse(string expression, out AlderExpression? result, out string? error)
    {
        ThrowIfDisposed();
        try
        {
            result = Parse(expression);
            error = null;
            return true;
        }
        catch (Exception ex) when (!ShouldRethrowTryApiException(ex))
        {
            result = null;
            error = ex.Message;
            return false;
        }
    }

    /// <inheritdoc cref="TryParse(string, out AlderExpression?, out string?)"/>
    public bool TryParse(string expression, out AlderExpression? result)
    {
        return TryParse(expression, out result, out _);
    }
}
