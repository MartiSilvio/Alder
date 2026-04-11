using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using Alder.Diagnostics;
using Alder.Interpretation;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.BreakStatement)]
internal static class BreakEmitter
{
    public static LinqExpression Emit(BoundBreakExpr node, EmissionContext ctx)
    {
        if (ctx.LoopDepth > 0 || ctx.SwitchDepth > 0)
            return LinqExpression.Assign(ctx.SignalParam, LinqExpression.Field(null, ControlFlowBreakField));

        return LinqExpression.Throw(
            LinqExpression.Constant(new AlderException(DiagnosticDescriptors.BreakOrContinueOutsideLoop)),
            typeof(ControlFlowSignal));
    }
}

[EmitsNode(BoundNodeKind.ContinueStatement)]
internal static class ContinueEmitter
{
    public static LinqExpression Emit(BoundContinueExpr node, EmissionContext ctx)
    {
        if (ctx.LoopDepth > 0)
            return LinqExpression.Assign(ctx.SignalParam, LinqExpression.Field(null, ControlFlowContinueField));

        return LinqExpression.Throw(
            LinqExpression.Constant(new AlderException(DiagnosticDescriptors.BreakOrContinueOutsideLoop)),
            typeof(ControlFlowSignal));
    }
}

[EmitsNode(BoundNodeKind.ReturnStatement)]
internal static class ReturnEmitter
{
    public static LinqExpression Emit(BoundReturnExpr node, EmissionContext ctx)
    {
        return LinqExpression.Assign(
            ctx.SignalParam,
            LinqExpression.Call(
                ControlFlowReturnMethod,
                node.Value == null
                    ? LinqExpression.Constant(null, typeof(object))
                    : ctx.EmitBoxed(node.Value)));
    }
}

[EmitsNode(BoundNodeKind.GotoStatement)]
internal static class GotoEmitter
{
    public static LinqExpression Emit(BoundGotoExpr node, EmissionContext ctx)
    {
        return LinqExpression.Assign(
            ctx.SignalParam,
            LinqExpression.Call(ControlFlowGotoMethod, LinqExpression.Constant(node.Label)));
    }
}

[EmitsNode(BoundNodeKind.GotoCaseStatement)]
internal static class GotoCaseEmitter
{
    public static LinqExpression Emit(BoundGotoCaseExpr node, EmissionContext ctx)
    {
        return LinqExpression.Assign(
            ctx.SignalParam,
            LinqExpression.Call(ControlFlowGotoCaseMethod, ctx.EmitBoxed(node.Value)));
    }
}
