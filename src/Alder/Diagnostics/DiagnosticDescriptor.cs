namespace Alder.Diagnostics;

/// <summary>
/// Pairs a <see cref="DiagnosticCode"/> with its Roslyn-matching message template.
/// Message templates use positional format arguments ({0}, {1}, etc.) matching Roslyn's format.
/// </summary>
/// <param name="Code">The diagnostic code.</param>
/// <param name="MessageTemplate">The message template with positional format arguments.</param>
public readonly record struct DiagnosticDescriptor(DiagnosticCode Code, string MessageTemplate)
{
    /// <summary>
    /// Formats the message template with the provided arguments.
    /// </summary>
    /// <param name="args">The format arguments to substitute into the template.</param>
    /// <returns>The formatted diagnostic message.</returns>
    public string FormatMessage(params object?[] args) => string.Format(MessageTemplate, args);
}
