namespace CsEval;

public class CsEvalException(string message) : Exception(message);

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