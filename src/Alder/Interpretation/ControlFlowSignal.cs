using Alder.Diagnostics;
using Alder.Text;

namespace Alder.Interpretation;

internal sealed class ControlFlowSignal
{
    public enum Kind { Return, Break, Continue, GotoCase, GotoDefault, Goto, YieldReturn, YieldBreak }
    public Kind SignalKind { get; }
    public object? Value { get; }
    public TextSpan Span { get; }
    private ControlFlowSignal(Kind kind, object? value = null, TextSpan span = default) { SignalKind = kind; Value = value; Span = span; }
    public static ControlFlowSignal Return(object? value) => new(Kind.Return, value);
    public static readonly ControlFlowSignal Break = new(Kind.Break);
    public static readonly ControlFlowSignal Continue = new(Kind.Continue);
    public static readonly ControlFlowSignal GotoDefaultSignal = new(Kind.GotoDefault);
    public static ControlFlowSignal GotoCaseSignal(object? value) => new(Kind.GotoCase, value);
    public static ControlFlowSignal GotoSignal(string label, TextSpan span) => new(Kind.Goto, label, span);
    public static ControlFlowSignal YieldReturnSignal(object? value) => new(Kind.YieldReturn, value);
    public static readonly ControlFlowSignal YieldBreakSignal = new(Kind.YieldBreak);

    public static object? UnwrapOrThrow(ControlFlowSignal? signal)
    {
        if (signal == null) return null;
        if (signal.SignalKind == Kind.Goto)
        {
            var ex = new AlderException(DiagnosticDescriptors.LabelNotFound, (string)signal.Value!);
            if (!signal.Span.IsEmpty)
                ex.EnrichDiagnosticsWithPosition(signal.Span, null, null);
            throw ex;
        }
        return signal.Value;
    }
}
