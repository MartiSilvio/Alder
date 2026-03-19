using CsEval.Diagnostics;
using CsEval.Text;

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
    public string? FormattedCode => ErrorCode?.ToDiagnosticId();

    /// <summary>
    /// Source span where the error occurred. Use <see cref="SourceText.GetLinePosition"/>
    /// to convert to line/column when needed.
    /// </summary>
    public TextSpan Span { get; internal set; }

    /// <summary>1-based line number, set when source text is available during enrichment.</summary>
    public int? Line { get; internal set; }

    /// <summary>1-based column number, set when source text is available during enrichment.</summary>
    public int? Column { get; internal set; }

    internal CsEvalException(string message) : base(message) { }

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
        return $"{descriptor.Code.ToDiagnosticId()}: {message}";
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
/// The type of execution limit that was exceeded.
/// </summary>
public enum ExecutionLimitType
{
    /// <summary>Maximum statement count exceeded.</summary>
    Statements,
    /// <summary>Maximum wall-clock timeout exceeded.</summary>
    Timeout
}

/// <summary>
/// Thrown when an execution resource limit is exceeded during evaluation.
/// Catchable independently of other CsEval exceptions.
/// The engine remains healthy after this exception -- subsequent evaluations work normally.
/// </summary>
public class CsEvalExecutionLimitException : CsEvalException
{
    /// <summary>Which limit was exceeded.</summary>
    public ExecutionLimitType LimitType { get; }

    /// <summary>The configured limit value (statement count or timeout milliseconds).</summary>
    public long LimitValue { get; }

    /// <summary>The actual value when the limit was hit.</summary>
    public long ActualValue { get; }

    /// <summary>Total statements executed when the exception was thrown.</summary>
    public long StatementsExecuted { get; }

    /// <summary>Wall-clock time elapsed when the exception was thrown. Zero if no timer was running.</summary>
    public TimeSpan ElapsedTime { get; }

    public CsEvalExecutionLimitException(
        ExecutionLimitType limitType, long limitValue, long actualValue,
        long statementsExecuted, TimeSpan elapsedTime)
        : base(FormatLimitMessage(limitType, limitValue, actualValue))
    {
        LimitType = limitType;
        LimitValue = limitValue;
        ActualValue = actualValue;
        StatementsExecuted = statementsExecuted;
        ElapsedTime = elapsedTime;
    }

    private static string FormatLimitMessage(ExecutionLimitType type, long limit, long actual) =>
        type switch
        {
            ExecutionLimitType.Statements => $"Execution exceeded maximum statement count ({limit}). {actual} statements executed.",
            ExecutionLimitType.Timeout => $"Execution exceeded maximum timeout ({limit}ms). {actual}ms elapsed.",
            _ => $"Execution limit exceeded: {type}"
        };
}

/// <summary>
/// Sentinel value for control flow signals (return, break, continue).
/// Not an Exception -- avoids expensive stack trace capture and SEH unwinding,
/// and prevents user catch blocks from intercepting internal control flow.
/// </summary>
internal sealed class ControlFlowSignal
{
    public enum Kind { Return, Break, Continue, GotoCase, GotoDefault, Goto }
    public Kind SignalKind { get; }
    public object? Value { get; }
    private ControlFlowSignal(Kind kind, object? value = null) { SignalKind = kind; Value = value; }
    public static ControlFlowSignal Return(object? value) => new(Kind.Return, value);
    public static readonly ControlFlowSignal Break = new(Kind.Break);
    public static readonly ControlFlowSignal Continue = new(Kind.Continue);
    public static readonly ControlFlowSignal GotoDefaultSignal = new(Kind.GotoDefault);
    public static ControlFlowSignal GotoCaseSignal(object? value) => new(Kind.GotoCase, value);
    public static ControlFlowSignal GotoSignal(string label) => new(Kind.Goto, label);
}
