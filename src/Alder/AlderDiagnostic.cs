using Alder.Diagnostics;
using Alder.Text;

namespace Alder;

/// <summary>
/// Structured diagnostic information from expression parsing, binding, or validation.
/// </summary>
/// <param name="Severity">The severity level of this diagnostic.</param>
/// <param name="Message">The human-readable diagnostic message.</param>
/// <param name="Code">The diagnostic code (e.g., a Roslyn CS code or an Alder ALDR code), or <c>null</c>.</param>
/// <param name="Span">The source text span where the diagnostic applies.</param>
/// <param name="Line">The one-based line number, or <c>null</c> if not available.</param>
/// <param name="Column">The one-based column number, or <c>null</c> if not available.</param>
public sealed record AlderDiagnostic(
    DiagnosticSeverity Severity,
    string Message,
    DiagnosticCode? Code = null,
    TextSpan Span = default,
    int? Line = null,
    int? Column = null)
{
    /// <summary>
    /// Gets the formatted diagnostic code string (e.g., <c>"CS0103"</c>), or <c>null</c> if no code is set.
    /// </summary>
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
/// Severity level for diagnostics emitted during expression processing.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>
    /// A fatal error that prevents evaluation.
    /// </summary>
    Error,

    /// <summary>
    /// A non-fatal warning about potentially incorrect usage.
    /// </summary>
    Warning,

    /// <summary>
    /// An informational message.
    /// </summary>
    Info
}
