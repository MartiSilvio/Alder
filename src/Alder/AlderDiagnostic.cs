using Alder.Diagnostics;
using Alder.Text;

namespace Alder;

/// <summary>
/// Structured diagnostic information from expression parsing or validation.
/// </summary>
public sealed record AlderDiagnostic(
    DiagnosticSeverity Severity,
    string Message,
    DiagnosticCode? Code = null,
    TextSpan Span = default,
    int? Line = null,
    int? Column = null)
{
    public string? FormattedCode => Code?.ToDiagnosticId();

    internal static AlderDiagnostic FromException(AlderException ex) =>
        ex.Diagnostics.Count > 0
            ? ex.Diagnostics[0]
            : new(DiagnosticSeverity.Error, ex.Message, ex.ErrorCode, ex.Span);

    internal static AlderDiagnostic FromException(Exception ex) => ex switch
    {
        AlderException csEx => FromException(csEx),
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
