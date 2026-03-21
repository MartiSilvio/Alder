using Alder.Diagnostics;
using Alder.Text;

namespace Alder;

public class AlderException : Exception
{
    public IReadOnlyList<AlderDiagnostic> Diagnostics { get; private set; }

    public DiagnosticCode? ErrorCode => Diagnostics.Count > 0 ? Diagnostics[0].Code : null;
    public string? FormattedCode => ErrorCode?.ToDiagnosticId();
    public TextSpan Span => Diagnostics.Count > 0 ? Diagnostics[0].Span : default;
    public int? Line => Diagnostics.Count > 0 ? Diagnostics[0].Line : null;
    public int? Column => Diagnostics.Count > 0 ? Diagnostics[0].Column : null;

    internal AlderException(string message) : base(message)
    {
        Diagnostics = [];
    }

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

public class AlderDepthException : AlderException
{
    public int MaxDepth { get; }

    public AlderDepthException(string subsystem, int maxDepth)
        : base($"Expression {subsystem} depth exceeded maximum of {maxDepth}. Configure AlderOptions.MaxExpressionDepth to adjust this limit.")
    {
        MaxDepth = maxDepth;
    }
}

public enum ExecutionLimitType
{
    Statements,
    Timeout
}

public class AlderExecutionLimitException : AlderException
{
    public ExecutionLimitType LimitType { get; }
    public long LimitValue { get; }
    public long ActualValue { get; }
    public long StatementsExecuted { get; }
    public TimeSpan ElapsedTime { get; }

    public AlderExecutionLimitException(
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
