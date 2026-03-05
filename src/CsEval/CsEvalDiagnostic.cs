using CsEval.Diagnostics;
using CsEval.Parsing;

namespace CsEval;

/// <summary>
/// Structured diagnostic information from expression parsing or validation.
/// </summary>
public sealed record CsEvalDiagnostic(
    DiagnosticSeverity Severity,
    string Message,
    DiagnosticCode? Code = null,
    int? Line = null,
    int? Column = null,
    int? SpanStart = null,
    int? SpanLength = null)
{
    internal static CsEvalDiagnostic FromException(CsEvalException ex) => ex switch
    {
        _ => new(DiagnosticSeverity.Error, ex.Message, ex.ErrorCode, ex.Line, ex.Column, ex.SpanStart, ex.SpanLength)
    };

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
