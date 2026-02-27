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
    int? Column = null)
{
    internal static CsEvalDiagnostic FromException(CsEvalException ex) => ex switch
    {
        CsEvalParserException pex => new(DiagnosticSeverity.Error, ex.Message, ex.ErrorCode, pex.Line, pex.Column),
        _ => new(DiagnosticSeverity.Error, ex.Message, ex.ErrorCode)
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
