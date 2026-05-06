namespace Alder.Diagnostics;

/// <summary>
/// Pairs a <see cref="DiagnosticCode"/> with its Roslyn-matching message template.
/// </summary>
public readonly record struct DiagnosticDescriptor(DiagnosticCode Code, string MessageTemplate)
{
    /// <summary>
    /// Formats the message template with the provided arguments.
    /// </summary>
    public string FormatMessage(params object?[] args) => string.Format(MessageTemplate, args);
}
