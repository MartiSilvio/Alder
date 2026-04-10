using Alder.Diagnostics;
using Alder.Text;

namespace Alder;

/// <summary>
/// Thrown when expression parsing, binding, or evaluation fails.
/// Contains structured <see cref="Diagnostics"/> with error codes, source positions, and messages.
/// </summary>
public class AlderException : Exception
{
    /// <summary>Structured diagnostics associated with this error.</summary>
    public IReadOnlyList<AlderDiagnostic> Diagnostics { get; private set; }

    /// <summary>Error code of the first diagnostic, or <c>null</c> if none are present.</summary>
    public DiagnosticCode? ErrorCode => Diagnostics.Count > 0 ? Diagnostics[0].Code : null;

    /// <summary>Formatted error code string (e.g., <c>"CS0103"</c>) of the first diagnostic, or <c>null</c>.</summary>
    public string? FormattedCode => ErrorCode?.ToDiagnosticId();

    /// <summary>Source text span where the error occurred.</summary>
    public TextSpan Span => Diagnostics.Count > 0 ? Diagnostics[0].Span : default;

    /// <summary>One-based line number where the error occurred, or <c>null</c> if unavailable.</summary>
    public int? Line => Diagnostics.Count > 0 ? Diagnostics[0].Line : null;

    /// <summary>One-based column number where the error occurred, or <c>null</c> if unavailable.</summary>
    public int? Column => Diagnostics.Count > 0 ? Diagnostics[0].Column : null;

    /// <param name="descriptor">The diagnostic descriptor providing the error code and message template.</param>
    /// <param name="args">Format arguments for the message template.</param>
    public AlderException(DiagnosticDescriptor descriptor, params object?[] args)
        : base(FormatMessage(descriptor, args))
    {
        Diagnostics = [new AlderDiagnostic(DiagnosticSeverity.Error, FormatMessage(descriptor, args), descriptor.Code)];
    }

    internal AlderException(DiagnosticDescriptor descriptor, TextSpan span, int? line, int? column, params object?[] args)
        : base(FormatMessage(descriptor, args))
    {
        Diagnostics = [new AlderDiagnostic(DiagnosticSeverity.Error, FormatMessage(descriptor, args), descriptor.Code, span, line, column)];
    }

    internal void EnrichDiagnosticsWithPosition(TextSpan span, int? line, int? column)
    {
        if (Diagnostics.Count > 0)
        {
            var enriched = Diagnostics[0] with { Span = span, Line = line, Column = column };
            Diagnostics = [enriched, ..Diagnostics.Skip(1)];
        }
        else
        {
            Diagnostics = [new AlderDiagnostic(DiagnosticSeverity.Error, Message, ErrorCode, span, line, column)];
        }
    }

    internal void SetDiagnostics(IReadOnlyList<AlderDiagnostic> diagnostics) => Diagnostics = diagnostics;

    private static string FormatMessage(DiagnosticDescriptor descriptor, object?[] args)
    {
        var message = descriptor.FormatMessage(args);
        return $"{descriptor.Code.ToDiagnosticId()}: {message}";
    }
}


/// <summary>
/// Identifies which execution limit was exceeded.
/// </summary>
public enum ExecutionLimitType
{
    /// <summary>Maximum statement count exceeded.</summary>
    Statements,

    /// <summary>Maximum evaluation time exceeded.</summary>
    Timeout,

    /// <summary>Maximum loop iteration count exceeded.</summary>
    LoopIterations
}

/// <summary>
/// Thrown when an execution constraint configured in <see cref="ExecutionConstraints"/> is exceeded.
/// </summary>
public class AlderExecutionLimitException : AlderException
{
    /// <summary>The type of limit that was exceeded.</summary>
    public ExecutionLimitType LimitType { get; }

    /// <summary>The configured limit value.</summary>
    public long LimitValue { get; }

    /// <summary>The actual value that exceeded the limit.</summary>
    public long ActualValue { get; }

    /// <summary>Total statements executed before the limit was hit.</summary>
    public long StatementsExecuted { get; }

    /// <summary>Elapsed wall-clock time when the limit was hit.</summary>
    public TimeSpan ElapsedTime { get; }

    /// <param name="limitType">The type of limit that was exceeded.</param>
    /// <param name="limitValue">The configured limit value.</param>
    /// <param name="actualValue">The actual value that exceeded the limit.</param>
    /// <param name="statementsExecuted">Total statements executed.</param>
    /// <param name="elapsedTime">Elapsed wall-clock time.</param>
    public AlderExecutionLimitException(
        ExecutionLimitType limitType, long limitValue, long actualValue,
        long statementsExecuted, TimeSpan elapsedTime)
        : base(GetDescriptor(limitType), limitValue, actualValue)
    {
        LimitType = limitType;
        LimitValue = limitValue;
        ActualValue = actualValue;
        StatementsExecuted = statementsExecuted;
        ElapsedTime = elapsedTime;
    }

    private static DiagnosticDescriptor GetDescriptor(ExecutionLimitType type) =>
        type switch
        {
            ExecutionLimitType.Statements => DiagnosticDescriptors.StatementLimitExceeded,
            ExecutionLimitType.Timeout => DiagnosticDescriptors.TimeoutExceeded,
            ExecutionLimitType.LoopIterations => DiagnosticDescriptors.LoopIterationLimitExceeded,
            _ => DiagnosticDescriptors.StatementLimitExceeded
        };
}
