using CsEval.Diagnostics;

namespace CsEval;

public class CsEvalException : Exception
{
    /// <summary>
    /// The C# compiler error code associated with this exception, or null for CsEval-specific errors.
    /// </summary>
    public DiagnosticCode? ErrorCode { get; }

    /// <summary>
    /// The error code formatted as a CS#### string (e.g., "CS0103"), or null if no error code.
    /// </summary>
    public string? FormattedCode => ErrorCode.HasValue ? $"CS{(int)ErrorCode.Value:D4}" : null;

    /// <summary>
    /// Backward-compatible constructor. Existing throw sites and subclasses continue to work.
    /// ErrorCode is null for exceptions created through this path.
    /// </summary>
    public CsEvalException(string message) : base(message) { }

    /// <summary>
    /// Diagnostic-aware constructor. Formats message as "CS####: {message}" and sets ErrorCode.
    /// </summary>
    public CsEvalException(DiagnosticDescriptor descriptor, params object?[] args)
        : base(FormatMessage(descriptor, args))
    {
        ErrorCode = descriptor.Code;
    }

    private static string FormatMessage(DiagnosticDescriptor descriptor, object?[] args)
    {
        var message = descriptor.FormatMessage(args);
        return $"CS{(int)descriptor.Code:D4}: {message}";
    }
}

/// <summary>
/// Thrown when expression nesting exceeds <see cref="CsEvalOptions.MaxExpressionDepth"/>.
/// Caught by the host process — unlike StackOverflowException, this is catchable.
/// </summary>
public class CsEvalDepthException : CsEvalException
{
    public int MaxDepth { get; }

    public CsEvalDepthException(string subsystem, int maxDepth)
        : base($"Expression {subsystem} depth exceeded maximum of {maxDepth}. Configure CsEvalOptions.MaxExpressionDepth to adjust this limit.")
    {
        MaxDepth = maxDepth;
    }
}

/// <summary>
/// Thrown when an Extended mode syntax feature is used with <see cref="LanguageMode.Standard"/>.
/// Extends <see cref="CsEvalParserException"/> so existing catch blocks still work.
/// Use this type specifically when you want to detect and handle language mode mismatches.
/// </summary>
/// <summary>
/// Thrown when an Extended mode syntax feature is used with <see cref="LanguageMode.Standard"/>.
/// Catchable independently of general <see cref="CsEvalException"/> or parser errors.
/// Use this type specifically when you want to detect and handle language mode mismatches,
/// e.g. to suggest enabling Extended mode to the user.
/// </summary>
public class CsEvalLanguageModeException : CsEvalException
{
    /// <summary>
    /// The name of the feature that requires Extended mode (e.g., "**", "in", "[:]", "..").
    /// </summary>
    public string FeatureName { get; }

    public CsEvalLanguageModeException(string featureName, string message)
        : base(message)
    {
        FeatureName = featureName;
    }
}

/// <summary>
/// Sentinel value for control flow signals (return, break, continue).
/// Not an Exception -- avoids expensive stack trace capture and SEH unwinding,
/// and prevents user catch blocks from intercepting internal control flow.
/// </summary>
internal sealed class ControlFlowSignal
{
    public enum Kind { Return, Break, Continue }
    public Kind SignalKind { get; }
    public object? Value { get; }
    private ControlFlowSignal(Kind kind, object? value = null) { SignalKind = kind; Value = value; }
    public static ControlFlowSignal Return(object? value) => new(Kind.Return, value);
    public static readonly ControlFlowSignal Break = new(Kind.Break);
    public static readonly ControlFlowSignal Continue = new(Kind.Continue);
}
