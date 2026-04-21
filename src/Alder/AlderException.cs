using Alder.Diagnostics;
using Alder.Text;

namespace Alder;

/// <summary>
/// Represents an Alder diagnostic failure.
/// Parsing, binding, validation, and evaluation all converge on this exception type so callers can inspect structured diagnostics.
/// </summary>
public class AlderException : Exception
{
    /// <summary>
    /// Gets the structured diagnostics associated with this failure.
    /// </summary>
    public IReadOnlyList<AlderDiagnostic> Diagnostics { get; private set; }

    /// <summary>
    /// Gets the code of the first diagnostic, if one exists.
    /// </summary>
    public DiagnosticCode? ErrorCode => Diagnostics.Count > 0 ? Diagnostics[0].Code : null;

    /// <summary>
    /// Gets the formatted identifier of the first diagnostic, for example <c>CS0103</c>.
    /// </summary>
    public string? FormattedCode => ErrorCode?.ToDiagnosticId();

    /// <summary>
    /// Gets the source span of the first diagnostic.
    /// </summary>
    public TextSpan Span => Diagnostics.Count > 0 ? Diagnostics[0].Span : default;

    /// <summary>
    /// Gets the one-based line number of the first diagnostic, if one is available.
    /// </summary>
    public int? Line => Diagnostics.Count > 0 ? Diagnostics[0].Line : null;

    /// <summary>
    /// Gets the one-based column number of the first diagnostic, if one is available.
    /// </summary>
    public int? Column => Diagnostics.Count > 0 ? Diagnostics[0].Column : null;

    /// <param name="descriptor">The diagnostic descriptor providing the error code and message template.</param>
    /// <param name="args">Format arguments for the message template.</param>
    public AlderException(DiagnosticDescriptor descriptor, params object?[] args)
        : base(FormatMessage(descriptor, args))
    {
        Diagnostics = [new AlderDiagnostic(DiagnosticSeverity.Error, FormatMessage(descriptor, args), descriptor.Code)];
    }

    /// <param name="descriptor">The diagnostic descriptor providing the error code and message template.</param>
    /// <param name="innerException">The original exception that triggered this diagnostic failure.</param>
    /// <param name="args">Format arguments for the message template.</param>
    public AlderException(DiagnosticDescriptor descriptor, Exception innerException, params object?[] args)
        : base(FormatMessage(descriptor, args), innerException)
    {
        Diagnostics = [new AlderDiagnostic(DiagnosticSeverity.Error, FormatMessage(descriptor, args), descriptor.Code)];
    }

    internal AlderException(DiagnosticDescriptor descriptor, TextSpan span, int? line, int? column, params object?[] args)
        : base(FormatMessage(descriptor, args))
    {
        Diagnostics = [new AlderDiagnostic(DiagnosticSeverity.Error, FormatMessage(descriptor, args), descriptor.Code, span, line, column)];
    }

    internal AlderException(
        DiagnosticDescriptor descriptor,
        TextSpan span,
        int? line,
        int? column,
        Exception innerException,
        params object?[] args)
        : base(FormatMessage(descriptor, args), innerException)
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

    internal static AlderException FromDiagnostics(IReadOnlyList<AlderDiagnostic> diagnostics)
    {
        if (diagnostics.Count == 0)
            return new AlderException(DiagnosticDescriptors.ExpressionExpected);

        return new AlderException(diagnostics);
    }

    private AlderException(IReadOnlyList<AlderDiagnostic> diagnostics)
        : base(diagnostics[0].Message)
    {
        Diagnostics = diagnostics;
    }

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
    /// <summary>
    /// Gets the type of limit that was exceeded.
    /// </summary>
    public ExecutionLimitType LimitType { get; }

    /// <summary>
    /// Gets the configured limit value.
    /// </summary>
    public long LimitValue { get; }

    /// <summary>
    /// Gets the observed value that exceeded the configured limit.
    /// </summary>
    public long ActualValue { get; }

    /// <summary>
    /// Gets the total statements executed before the limit was hit.
    /// </summary>
    public long StatementsExecuted { get; }

    /// <summary>
    /// Gets the elapsed wall-clock time when the limit was hit.
    /// </summary>
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
