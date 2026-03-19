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
    TextSpan Span = default)
{
    internal static CsEvalDiagnostic FromException(CsEvalException ex) =>
        new(DiagnosticSeverity.Error, ex.Message, ex.ErrorCode, ex.Span);

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
