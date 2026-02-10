namespace CsEval.Diagnostics;

/// <summary>
/// Pairs a <see cref="DiagnosticCode"/> with its Roslyn-matching message template.
/// Message templates use positional format arguments ({0}, {1}, etc.) matching Roslyn's format.
/// </summary>
public readonly record struct DiagnosticDescriptor(DiagnosticCode Code, string MessageTemplate)
{
    /// <summary>
    /// Formats the message template with the provided arguments.
    /// </summary>
    public string FormatMessage(params object?[] args) => string.Format(MessageTemplate, args);
}
