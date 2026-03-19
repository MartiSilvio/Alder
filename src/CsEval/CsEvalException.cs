using CsEval.Diagnostics;
using CsEval.Text;

namespace CsEval;

public class CsEvalException : Exception
{
    public IReadOnlyList<CsEvalDiagnostic> Diagnostics { get; private set; }

    public DiagnosticCode? ErrorCode => Diagnostics.Count > 0 ? Diagnostics[0].Code : null;
    public string? FormattedCode => ErrorCode?.ToDiagnosticId();
    public TextSpan Span => Diagnostics.Count > 0 ? Diagnostics[0].Span : default;
    public int? Line => Diagnostics.Count > 0 ? Diagnostics[0].Line : null;
    public int? Column => Diagnostics.Count > 0 ? Diagnostics[0].Column : null;

    internal CsEvalException(string message) : base(message)
    {
        Diagnostics = [];
    }

    public CsEvalException(DiagnosticDescriptor descriptor, params object?[] args)
        : base(FormatMessage(descriptor, args))
    {
        Diagnostics = [new CsEvalDiagnostic(DiagnosticSeverity.Error, FormatMessage(descriptor, args), descriptor.Code)];
    }

    internal CsEvalException(DiagnosticDescriptor descriptor, TextSpan span, int? line, int? column, params object?[] args)
        : base(FormatMessage(descriptor, args))
    {
        Diagnostics = [new CsEvalDiagnostic(DiagnosticSeverity.Error, FormatMessage(descriptor, args), descriptor.Code, span, line, column)];
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
            Diagnostics = [new CsEvalDiagnostic(DiagnosticSeverity.Error, Message, ErrorCode, span, line, column)];
        }
    }

    internal void SetDiagnostics(IReadOnlyList<CsEvalDiagnostic> diagnostics) => Diagnostics = diagnostics;

    private static string FormatMessage(DiagnosticDescriptor descriptor, object?[] args)
    {
        var message = descriptor.FormatMessage(args);
        return $"{descriptor.Code.ToDiagnosticId()}: {message}";
    }
}

public class CsEvalDepthException : CsEvalException
{
    public int MaxDepth { get; }

    public CsEvalDepthException(string subsystem, int maxDepth)
        : base($"Expression {subsystem} depth exceeded maximum of {maxDepth}. Configure CsEvalOptions.MaxExpressionDepth to adjust this limit.")
    {
        MaxDepth = maxDepth;
    }
}

public enum ExecutionLimitType
{
    Statements,
    Timeout
}

public class CsEvalExecutionLimitException : CsEvalException
{
    public ExecutionLimitType LimitType { get; }
    public long LimitValue { get; }
    public long ActualValue { get; }
    public long StatementsExecuted { get; }
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
