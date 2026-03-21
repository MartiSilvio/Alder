namespace Alder.Interpretation;

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
