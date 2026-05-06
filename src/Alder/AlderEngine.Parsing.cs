using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder;

public sealed partial class AlderEngine
{
    private readonly record struct ParseAttempt(
        AlderExpression? Expression,
        IReadOnlyList<AlderDiagnostic> Diagnostics)
    {
        internal bool Success => Expression != null && Diagnostics.Count == 0;
    }

    private ParseAttempt ParseCore(string expression)
    {
        var diagnostics = new AlderDiagnosticBag();

        try
        {
            var lexer = new Lexer(expression);
            var tokens = lexer.Tokenize();

            var parser = ExpressionParser.CreateForSubExpression(tokens, _config.LanguageMode);
            var ast = parser.Parse();

            return new ParseAttempt(new AlderExpression(expression, ast), diagnostics.ToReadOnly());
        }
        catch (InsufficientExecutionStackException ex)
        {
            diagnostics.Add(new AlderException(DiagnosticDescriptors.ExpressionNestingDepthExceeded, ex));
        }
        catch (Exception ex) when (!ShouldRethrowTryApiException(ex))
        {
            diagnostics.Add(ex);
        }

        return new ParseAttempt(null, diagnostics.ToReadOnly());
    }

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
        var attempt = ParseCore(expression);
        if (attempt.Success)
            return attempt.Expression!;

        throw AlderException.FromDiagnostics(attempt.Diagnostics);
    }

    /// <summary>
    /// Attempts to parse source without throwing for ordinary parse failures.
    /// </summary>
    /// <param name="expression">Expression source to parse.</param>
    /// <param name="result">When successful, the parsed expression; otherwise, <c>null</c>.</param>
    /// <param name="diagnostics">When parsing fails, the structured diagnostics; otherwise, an empty list.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public bool TryParse(string expression, out AlderExpression? result, out IReadOnlyList<AlderDiagnostic> diagnostics)
    {
        if (expression is null) throw new ArgumentNullException(nameof(expression));
        ThrowIfDisposed();
        var attempt = ParseCore(expression);
        result = attempt.Expression;
        diagnostics = attempt.Diagnostics;
        return attempt.Success;
    }
}
