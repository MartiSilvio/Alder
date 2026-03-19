using CsEval.Diagnostics;
using CsEval.Text;

namespace CsEval;

/// <summary>
/// Structured diagnostic information from expression parsing or validation.
/// </summary>
public sealed record CsEvalDiagnostic(
    DiagnosticSeverity Severity,
    string Message,
    DiagnosticCode? Code = null,
    TextSpan Span = default,
    int? Line = null,
    int? Column = null)
{
    public string? FormattedCode => Code?.ToDiagnosticId();

    internal static CsEvalDiagnostic FromException(CsEvalException ex) =>
        ex.Diagnostics.Count > 0
            ? ex.Diagnostics[0]
            : new(DiagnosticSeverity.Error, ex.Message, ex.ErrorCode, ex.Span);

    internal static CsEvalDiagnostic FromException(Exception ex) => ex switch
    {
        CsEvalException csEx => FromException(csEx),
        _ => new(DiagnosticSeverity.Error, ex.Message)
    };
}

/// <summary>
/// Severity level for diagnostics.
/// </summary>
public enum DiagnosticSeverity
{
    Error,
    Warning,
    Info
}
